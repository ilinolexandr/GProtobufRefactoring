<#
.SYNOPSIS
    Generate a markdown report of benchmark performance gaps.

.DESCRIPTION
    Scans BenchmarkDotNet CSV artifacts and emits a markdown document with
    three sections, each listing cases where one engine is slower than another
    by at least the configured ratio threshold:
        1. OnePass slower than TwoPass_Stream
        2. OnePass slower than protobuf-net
        3. TwoPass slower than protobuf-net
    A glossary at the end describes each referenced benchmark class (pulled
    from GProtobuf.Benchmark/BENCHMARKS.md so the docs stay single-source).

.PARAMETER ResultsDir
    BDN results directory. Default: BenchmarkDotNet.Artifacts/results

.PARAMETER DocSource
    Source of benchmark descriptions. Default: GProtobuf.Benchmark/BENCHMARKS.md

.PARAMETER Output
    Output markdown file. Default: BENCHMARK_GAPS.md

.PARAMETER Threshold
    Ratio threshold (slower/faster). Rows where ratio >= threshold appear in
    the tables. Default 1.5.

.EXAMPLE
    powershell -File tools/bench-gaps.ps1
    powershell -File tools/bench-gaps.ps1 -Threshold 2.0 -Output BENCHMARK_GAPS_STRICT.md
#>
param(
    [string]$ResultsDir = 'BenchmarkDotNet.Artifacts/results',
    [string]$DocSource  = 'GProtobuf.Benchmark/BENCHMARKS.md',
    [string]$Output     = 'BENCHMARK_GAPS.md',
    [double]$Threshold  = 1.5
)

$ErrorActionPreference = 'Stop'
$ci = [System.Globalization.CultureInfo]::InvariantCulture

# Unicode characters for pretty output. Declared explicitly so this .ps1 file
# remains pure ASCII (PowerShell 5.1 reads .ps1 as Windows-1252 without BOM,
# so inline unicode in source would break). The characters are fine in runtime
# .NET string content.
$MU  = [char]0x03BC    # Greek small letter mu
$GE  = [char]0x2265    # greater-than-or-equal
$TIMES = [char]0x00D7  # multiplication sign
$EMDASH = [char]0x2014 # em dash

function Parse-Time([string]$s) {
    if (-not $s -or $s -eq 'NA') { return $null }
    # Matches BDN Mean column format: "6.800 us" / "6.800 <mu>s" / "1.2 ns" / etc.
    $m = [regex]::Match($s, '^\s*([0-9.,]+)\s+(ns|\u03BCs|us|ms|s)\s*$')
    if (-not $m.Success) { return $null }
    $v = [double]::Parse($m.Groups[1].Value, $ci)
    $unit = $m.Groups[2].Value
    if ($unit -eq 'ns') { return $v }
    if ($unit -eq 'ms') { return $v * 1e6 }
    if ($unit -eq 's')  { return $v * 1e9 }
    # us or <mu>s
    return $v * 1e3
}

function Parse-Bytes([string]$s) {
    if (-not $s -or $s -eq 'NA') { return $null }
    $m = [regex]::Match($s, '^\s*([0-9.,]+)\s*B\s*$')
    if (-not $m.Success) { return $null }
    [long]([double]::Parse($m.Groups[1].Value, $ci))
}

# Parse BENCHMARKS.md. Format per entry:
#   #### `BenchmarkClassName`
#   One-or-more-paragraph description...
# Entry ends at next ####, ###, ---, or end of file.
function Read-Descriptions([string]$path) {
    $dict = [ordered]@{}
    if (-not (Test-Path $path)) {
        Write-Warning "$path not found -- glossary will be empty."
        return $dict
    }
    # Force UTF-8: PS 5.1 defaults to Windows-1252 which mangles arrows/em-dashes.
    $text = Get-Content $path -Raw -Encoding UTF8
    $re = [regex]'(?ms)^####\s+`([^`]+)`\s*\r?\n(.*?)(?=^####\s+`|^###\s+|^---|\z)'
    foreach ($m in $re.Matches($text)) {
        $name = $m.Groups[1].Value.Trim()
        $body = $m.Groups[2].Value.Trim()
        # Take first paragraph (up to first blank line), collapse whitespace.
        $firstPara = ($body -split '(?m)^\s*$', 2)[0].Trim() -replace '\s+', ' '
        $dict[$name] = $firstPara
    }
    return $dict
}

if (-not (Test-Path $ResultsDir)) {
    Write-Error "Results dir not found: $ResultsDir. Run benchmarks first."
    exit 1
}
$files = Get-ChildItem "$ResultsDir/*-report.csv"
if (-not $files) {
    Write-Error "No *-report.csv under $ResultsDir."
    exit 1
}

$descs = Read-Descriptions $DocSource

# Flatten every (Type, Params) into a single row with all three engine means.
$data = foreach ($file in $files) {
    $typeName = ($file.BaseName -replace '-report$', '').Split('.')[-1]
    $rows = Import-Csv $file.FullName
    if (-not $rows) { continue }

    $headers = $rows[0].PSObject.Properties.Name
    $catIdx  = [Array]::IndexOf($headers, 'Categories')
    $meanIdx = [Array]::IndexOf($headers, 'Mean')
    if ($catIdx -lt 0 -or $meanIdx -lt 0) { continue }
    $paramCols = if ($meanIdx -gt $catIdx + 1) { $headers[($catIdx+1)..($meanIdx-1)] } else { @() }

    $groups = if ($paramCols.Count -eq 0) {
        @([pscustomobject]@{ Name = '-'; Group = $rows })
    } else {
        $rows | Group-Object -Property $paramCols
    }

    foreach ($g in $groups) {
        $one = $g.Group | Where-Object { $_.Method -eq 'GProtobuf_OnePass_Ser' } | Select-Object -First 1
        $two = $g.Group | Where-Object { $_.Method -eq 'GProtobuf_TwoPass_Stream_Ser' } | Select-Object -First 1
        $pn  = $g.Group | Where-Object { $_.Method -eq 'ProtobufNet_Ser' } | Select-Object -First 1

        [pscustomobject]@{
            Type       = $typeName
            Params     = $g.Name
            OnePass_ns = if ($one) { Parse-Time $one.Mean } else { $null }
            TwoPass_ns = if ($two) { Parse-Time $two.Mean } else { $null }
            PbNet_ns   = if ($pn)  { Parse-Time $pn.Mean }  else { $null }
        }
    }
}

function Get-SectionRows($slowKey, $fastKey) {
    $data | Where-Object {
        $_.$slowKey -and $_.$fastKey -and $_.$fastKey -gt 0 -and
        ($_.$slowKey / $_.$fastKey) -ge $Threshold
    } | ForEach-Object {
        [pscustomobject]@{
            Type    = $_.Type
            Params  = $_.Params
            SlowUs  = [math]::Round($_.$slowKey / 1e3, 2)
            FastUs  = [math]::Round($_.$fastKey / 1e3, 2)
            Ratio   = [math]::Round($_.$slowKey / $_.$fastKey, 2)
        }
    } | Sort-Object Ratio -Descending
}

function Render-Section($title, $slowLabel, $fastLabel, $rows, $typesUsed) {
    $sb = [System.Text.StringBuilder]::new()
    [void]$sb.AppendLine("## $title")
    [void]$sb.AppendLine()
    if (-not $rows -or $rows.Count -eq 0) {
        [void]$sb.AppendLine("_No cases above $Threshold$TIMES threshold._")
        [void]$sb.AppendLine()
        return $sb.ToString()
    }
    [void]$sb.AppendLine("| Benchmark | Params | $slowLabel (${MU}s) | $fastLabel (${MU}s) | Ratio |")
    [void]$sb.AppendLine("|-----------|--------|-------------------:|-------------------:|------:|")
    foreach ($r in $rows) {
        $anchor = $r.Type.ToLower()
        $paramCell = if ($r.Params) { $r.Params } else { $EMDASH }
        [void]$sb.AppendLine("| [``$($r.Type)``](#$anchor) | $paramCell | $($r.SlowUs) | $($r.FastUs) | $($r.Ratio)$TIMES |")
        $typesUsed[$r.Type] = $true
    }
    [void]$sb.AppendLine()
    return $sb.ToString()
}

$typesUsed = @{}
$s1rows = Get-SectionRows 'OnePass_ns' 'TwoPass_ns'
$s2rows = Get-SectionRows 'OnePass_ns' 'PbNet_ns'
$s3rows = Get-SectionRows 'TwoPass_ns' 'PbNet_ns'

$s1 = Render-Section '1. OnePass slower than TwoPass_Stream' 'OnePass' 'TwoPass' $s1rows $typesUsed
$s2 = Render-Section '2. OnePass slower than protobuf-net'   'OnePass' 'PbNet'   $s2rows $typesUsed
$s3 = Render-Section '3. TwoPass slower than protobuf-net'   'TwoPass' 'PbNet'   $s3rows $typesUsed

$glossary = [System.Text.StringBuilder]::new()
if ($typesUsed.Count -gt 0) {
    [void]$glossary.AppendLine("## Benchmark glossary")
    [void]$glossary.AppendLine()
    foreach ($name in ($typesUsed.Keys | Sort-Object)) {
        $d = if ($descs.Contains($name)) { $descs[$name] } else { '_(no description in BENCHMARKS.md -- update the doc)_' }
        [void]$glossary.AppendLine("### ``$name``")
        [void]$glossary.AppendLine()
        [void]$glossary.AppendLine($d)
        [void]$glossary.AppendLine()
    }
}

$timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm'
$header = "# Benchmark performance gaps`r`n`r`n_Generated: $timestamp from ``$ResultsDir``. Threshold: ratio $GE $Threshold$TIMES. Benchmark class names link to the glossary below._`r`n`r`n"

$content = $header + $s1 + $s2 + $s3 + $glossary.ToString()

# Write UTF-8 with BOM for broad editor compatibility on Windows.
$utf8Bom = New-Object System.Text.UTF8Encoding($true)
[System.IO.File]::WriteAllText([System.IO.Path]::GetFullPath($Output), $content, $utf8Bom)

Write-Host ("Wrote {0}: sec1={1}, sec2={2}, sec3={3}, {4} unique benchmarks." `
    -f $Output, $s1rows.Count, $s2rows.Count, $s3rows.Count, $typesUsed.Count)
