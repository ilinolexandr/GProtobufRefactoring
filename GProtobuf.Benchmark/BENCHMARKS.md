# GProtobuf Benchmark Suite

Comprehensive reference for the benchmark suite under `GProtobuf.Benchmark/`.

Purpose: track performance and allocation changes across data types,
encodings, and write-path variants (OnePass / TwoPass) so optimisations
can be measured and regressions caught early.

- **17 benchmark classes** · 5 folders · ~300+ test cases
- **Baseline:** protobuf-net 3.2.46 (except Inheritance — see R-009)
- **Canonical within-GProtobuf pairing:** `GProtobuf_OnePass_Ser` vs
  `GProtobuf_TwoPass_Stream_Ser` — same `Stream` sink, apples-to-apples.
  OnePass will become the default write path; main-set benches are
  structured to make that ratio readable at a glance.
- **Regression backlog:** see [`BENCHMARK_REGRESSIONS.md`](../BENCHMARK_REGRESSIONS.md)

---

## Quickstart

```powershell
# Run everything (defaults to dev profile, in-process, 1 warmup + 3 iters)
dotnet run --project GProtobuf.Benchmark -c Release

# One class
dotnet run --project GProtobuf.Benchmark -c Release -- --filter "*Primitive*"

# Several classes
dotnet run --project GProtobuf.Benchmark -c Release -- --filter "*Dictionary*" "*Inheritance*"

# List all benchmark methods without running
dotnet run --project GProtobuf.Benchmark -c Release -- --list flat

# CI-grade measurements (2 warmup + 5 iters, subprocess toolchain)
dotnet run --project GProtobuf.Benchmark -c Release -- --profile ci-fast --filter "*"

# Full profile — weekly / manual (3 warmup + 10 iters)
dotnet run --project GProtobuf.Benchmark -c Release -- --profile full --filter "*"
```

Without `--filter`, the Program auto-appends `--filter *` so non-TTY
runs don't hang on BDN's interactive prompt.

---

## Profile modes

Selected by `GPROTOBUF_BENCH_MODE` env var or `--profile <mode>` flag.

| Profile  | Toolchain            | Warmup | Iters | Use when                       |
|----------|----------------------|:------:|:-----:|--------------------------------|
| `dev`    | InProcessEmit (fast) | 1      | 3     | Quick local feedback           |
| `ci-fast`| Default (subprocess) | 2      | 5     | PR gate / scheduled CI         |
| `full`   | Default (subprocess) | 3      | 10    | Weekly / release baselines     |

`dev` trades accuracy for turnaround and dodges Windows Defender noise
on subprocess spawn. Don't trust dev numbers for absolute measurements
— they're for relative direction only.

---

## Folder layout

```
GProtobuf.Benchmark/
├── Infrastructure/             Shared config, helpers, base class
│   ├── BenchmarkProfile.cs     (enum) Dev | CiFast | Full
│   ├── BenchmarkConfig.cs      Resolves profile → BenchmarkDotNet config
│   ├── BenchmarkBase.cs        SerdeBenchmarkBase<T> — paired Ser/De/SizeCalc
│   ├── OnePassHarness.cs       8 KB reusable temp buffer + shared pool
│   ├── PayloadSizeColumn.cs    Reflection-based Bytes column
│   └── TestDataFactory.cs      Deterministic edge-case data factories
│
├── Scalars/                    Primitive-type benches
├── Collections/                Lists / arrays / dictionaries
├── Messages/                   Nested / polymorphic / deeply-nested
├── OnePassSpecific/            Targeted OnePass allocation/nesting scans
├── TwoPassSpecific/            TwoPass-only variants (sink, prepass, etc.)
│
├── Models/                     Dual-attribute DTOs consumed by benches
│   ├── PrimitiveTypesModel.cs      (+ header explaining dual-attribute rule)
│   ├── CollectionsModel.cs
│   ├── NestedMessagesModel.cs
│   ├── ObjectArrayBuilderTestModel.cs
│   ├── NestedDictionaryModel.cs    (dual attrs NOT yet applied)
│   ├── NestedTupleModel.cs         (unused)
│   ├── Scalars/                    Int32*, String*, Bytes* edge-case models
│   ├── Collections/                DictionaryIntStringModel
│   └── Messages/                   DeepNestedNode, MinimalMessageModel, InheritanceModels
│
├── GProtobufAssemblyOptions.cs     [assembly: GProtobufOptions(GenerateOnePassStreamWriter=true, UseStringPooling=true)]
├── Program.cs                      BenchmarkSwitcher.FromAssembly(...)
└── BENCHMARKS.md                   (this file)
```

---

## Benchmark inventory

Every benchmark class inherits `SerdeBenchmarkBase<T>` and therefore
exposes the same method surface (unless otherwise noted):

| Category         | Methods                                                         |
|------------------|-----------------------------------------------------------------|
| **Serialize**    | `ProtobufNet_Ser` (baseline), `GProtobuf_OnePass_Ser`, `GProtobuf_TwoPass_Stream_Ser` |
| **Deserialize**  | `ProtobufNet_De` (baseline), `GProtobuf_De`                     |
| **SizeCalc**     | `GProtobuf_SizeCalc` (TwoPass prepass — has no OnePass analogue) |

The main serialize-set has exactly **three** rows on purpose: protobuf-net
reference, OnePass writing to Stream, TwoPass writing to Stream. All three
share the same sink so Ratio columns read directly as engine-vs-engine.
The alternative TwoPass sink (`ArrayBufferWriter<byte>`) is measured
separately in [`TwoPassSpecific/BufferWriterVsStreamBenchmark`](#twopassspecific)
— it's a TwoPass-internal choice, not a fair cross-engine comparison.

All methods return a numeric (payload bytes or count) to defeat dead-code
elimination. The `Bytes` column picks up the `Serialized` byte[] length
via reflection over the benchmark instance.

### Scalars/

#### `PrimitiveTypesBenchmark`
Reference: single model with all primitive fields at once (int, long, float,
double, bool, string, byte[], Fixed/ZigZag variants). Fixed data, one row
per method. Payload ≈ 143 B.

#### `Int32DefaultBenchmark`
100 × int32 values, default varint encoding (non-packed).
`[Params(Shape)]` = { `Zero`, `Boundary1Byte` (127), `Boundary2Byte` (128),
`MaxValue`, `NegativeOne` (-1 → 10-byte varint!), `MinValue` }.
Exposes varint-length boundaries and the -1 pathological case.

#### `Int32ZigZagBenchmark`
Same 100-element list, **ZigZag** encoding. Same shape params. Negative
values are where ZigZag wins (2-byte vs 10-byte).

#### `Int32FixedBenchmark`
Same 100-element list, **Fixed32** encoding (packed). Payload size is
invariant across Shape (always ~403 B), so Mean should also be roughly
flat — a consistency check plus a per-value-cost baseline vs varint.

#### `StringEdgeCasesBenchmark`
50 × string of uniform shape.
`[Params(Shape)]` = { `Empty`, `OneCharAscii`, `Short63Ascii`,
`Boundary128Ascii`, `Long1KAscii`, `ShortUtf8Multibyte`, `LongUtf8Multibyte` }.
Sweeps varint-length-prefix boundaries (63/64, 127/128) and
ASCII-vs-UTF-8 encoding cost.

#### `BytesEdgeCasesBenchmark`
Single byte[] payload.
`[Params(Size)]` = { 0, 1, 64, 1024, 65536, 1048576 }.
Covers stackalloc → ArrayPool → LOH boundaries.

---

### Collections/

#### `CollectionsBenchmark`
Mixed-type model: `List<int|long|float|double|string>`, `int[]`, `string[]`,
`List<int>` with Packed/Fixed, `List<int>` with ZigZag. Fixed size, single row
per method. Real-world shape.

#### `PackedInt32ScalingBenchmark`
Reuses `Int32FixedListModel`, but parameterised by N.
`[Params(N)]` = { 0, 1, 10, 1000, 100000 }. Scaling curve for packed fixed32.

#### `DictionaryScalingBenchmark`
`Dictionary<int, string>`.
`[Params(N)]` = { 0, 1, 10, 100, 1000 }. Each entry is a length-delimited
map-entry sub-message — per-entry framing cost dominates at mid N.

---

### Messages/

#### `NestedMessagesBenchmark`
`NestedMessagesModel` — `PersonModel`, `AddressModel`, `List<PersonModel>`,
`CompanyModel` with its own sub-collections. Realistic nested shape.

#### `ObjectArrayBuilderBenchmark`
Arrays of message objects. `[Params(ItemCount)]` = { 10, 50, 100, 500 }.
Originally designed to demonstrate `ObjectArrayBuilder` allocation reduction
during array-of-message deserialisation.

Note: adds two deserialise variants:
- `GProtobuf_De_Span(ReadOnlySpan<byte>)`
- `GProtobuf_De_Stream(Stream)`

#### `DeepNestedMessageBenchmark`
Linear chain of self-referential `DeepNestedNode`.
`[Params(Depth)]` = { 1, 5, 10, 25 }. Depth=25 exceeds the OnePass
inline-stack cap of 16 → OnePass_Ser is NA (see R-007). TwoPass is unaffected.

#### `EmptyMessageBenchmark`
`MinimalMessageModel` with one `int` field set to its default (protobuf
skips default values, so wire payload is 0 bytes). Measures pure library
dispatch overhead — use as a floor when comparing richer benches.

#### `InheritanceBenchmark`
3-level hierarchy `InheritBase → InheritDerived → InheritDoublyDerived`
with `[ProtoInclude]`.
`[Params(Level)]` = { 1, 2, 3 } — instantiates at each depth.

Non-standard baseline: `GProtobuf_TwoPass_Stream_Ser` is `Baseline = true`
(not protobuf-net) because R-009 prevents dual `[ProtoInclude]`, so
protobuf-net cannot do polymorphic dispatch. ProtobufNet rows are
reference upper-bounds, NOT apples-to-apples.

---

### OnePassSpecific/

#### `TempBufferSizeBenchmark`
Fixed model (`Int32DefaultListModel` × 100 × NegativeOne), variable
OnePass temp buffer.
`[Params(BufferSize)]` = { 256, 1024, 4096, 8192, 16384, 65536 }.
Designed to validate R-002 hypothesis. Current result: allocation is 64 B
at every buffer size, so R-002 is flagged for re-test under ci-fast.

Only OnePass is measured; TwoPass_Stream is included as an invariance
reference (its cost doesn't depend on BufferSize).

#### `NestingOverheadBenchmark`
Fine-grained Depth scan of `DeepNestedNode`.
`[Params(Depth)]` = { 1, 2, 4, 8, 12, 16 }. Isolates per-level
`BeginSubMessage` / `BufferChainPool.Rent` cost. Confirmed R-011:
OnePass rents one fixed 8 KB pool buffer per serialization for shallow
hierarchies; additional rentals accumulate past depth 12.

Baseline is `GProtobuf_TwoPass_Stream_Ser` so the ratio column shows
OnePass overhead directly.

---

### TwoPassSpecific/

#### `BufferWriterVsStreamBenchmark`
Isolates the TwoPass write-sink choice: `MemoryStream` vs
`ArrayBufferWriter<byte>`. Both share the same SizeCalc prepass — the
difference is purely how the second pass emits bytes.
`[Params(N)]` = { 10, 100, 1000, 10000 } over `Int32DefaultListModel`
filled with `VarintShape.MaxValue` (5-byte varints → payload spans
~50 B → ~50 KB, crossing the 8 KB temp-buffer boundary both ways).

Only two serialize methods, no deserialize, no protobuf-net. Baseline is
`GProtobuf_TwoPass_Stream_Ser` so the ratio column shows the
BufferWriter-vs-Stream sink delta directly.

Deliberately removed from the main serialize-set (the 3-row canonical
surface) so that OnePass-vs-TwoPass comparisons aren't diluted by an
intra-TwoPass sink variant. Add new bench classes here if a second
TwoPass-specific axis appears (e.g. a SizeCalc-only observability scan).

---

## Infrastructure

### `SerdeBenchmarkBase<T>`

Abstract base. Concrete bench supplies:
- `BuildModel()` — model factory (called once per param combo in GlobalSetup).
- `PreSerialize(T model)` — produces the canonical byte[] fed to deserialise
  benches and reflected by `PayloadSizeColumn`.

Provides:
- `Model` — the model under test
- `Serialized` — pre-serialised bytes
- `Stream` — reusable 64 KB `MemoryStream`
- `BufferWriter` — reusable `ArrayBufferWriter<byte>`
- `[IterationSetup]` — resets Stream.Position / Length and `BufferWriter.WrittenCount`
- `[GlobalSetup]` — builds model, pre-serialises, runs `WarmupProtoBufNet`

`WarmupProtoBufNet` swallows exceptions intentionally: on models where
protobuf-net lacks `[ProtoBuf.ProtoInclude]` (see R-009) the warmup
would otherwise throw and kill GlobalSetup for the whole class. Failures
are deferred to individual benchmark rows which show NA.

### `OnePassHarness`

Reusable 8 KB `[ThreadStatic]` temp buffer + `BufferChainPoolCache.Shared`.
Every OnePass bench uses it via:

```csharp
using var scope = new OnePassScope(OnePassHarness.Pool);
var writer = new OnePassStreamWriter(Stream, OnePassHarness.TempBuffer, scope.Pool);
Models.{Ns}.Serialization.OnePassStreamWriters.Write{Type}(ref writer, Model);
writer.Flush();
```

Mirrors the real high-perf call pattern used in production (see
`FrozenDictBenchmark/DispatchBenchmark.cs`). Avoids the 256-byte
stackalloc used by the generated convenience wrappers, which are meant
for incidental use rather than tight loops.

### `TestDataFactory`

Deterministic shape-to-value factories. Single `Random(42)` seed.

- `VarintShape` enum: `Zero, One, Boundary1Byte, Boundary2Byte, Boundary2To3, MaxValue, NegativeOne, MinValue`
- `StringShape` enum: `Empty, OneCharAscii, Short63Ascii, Boundary64Ascii, Short127Ascii, Boundary128Ascii, Long1KAscii, Long64KAscii, ShortUtf8Multibyte, LongUtf8Multibyte`
- `Int32(shape)`, `Int64(shape)`, `String(shape)`, `Bytes(size)`, `IntArray(n, shape)`

Used as `[Params(...)]` values directly.

### `PayloadSizeColumn`

BDN custom column showing pre-serialised payload size in bytes.

Implementation note: BDN runs benchmark cases in a child process, so an
in-memory registry populated by `[GlobalSetup]` is invisible to the
summary-rendering parent process. The column instead reflects over the
benchmark type, instantiates it, invokes `[GlobalSetup]`, and reads
the protected `Serialized` field. Cached per `(type, paramsDisplay)`.

---

## Output columns

Default summary table layout:

| Column           | Meaning                                                               |
|------------------|-----------------------------------------------------------------------|
| **Method**       | Benchmark method name                                                 |
| **Categories**   | `Serialize` / `Deserialize` / `SizeCalc` (+ `CiFast` / `OnePassSpecific`) |
| **Mean / Error / StdDev / Median** | Standard BDN timing stats                         |
| **Ratio / RatioSD** | Time relative to the Baseline method in the same category          |
| **Bytes**        | Pre-serialised payload size (read from `Serialized.Length`)           |
| **Allocated**    | Allocated memory per op (MemoryDiagnoser)                             |
| **Alloc Ratio**  | Allocation relative to baseline                                       |

Per-class baselines:
- Most classes: `ProtobufNet_Ser` / `ProtobufNet_De` are baselines.
- `InheritanceBenchmark`: `GProtobuf_TwoPass_Stream_Ser` and `GProtobuf_De`
  are baselines (R-009 workaround).
- `TempBufferSizeBenchmark`: `GProtobuf_OnePass_Ser` is baseline
  (only OnePass matters for that scan).
- `NestingOverheadBenchmark`: `GProtobuf_TwoPass_Stream_Ser` is baseline
  (surfaces OnePass overhead ratio directly).
- `BufferWriterVsStreamBenchmark`: `GProtobuf_TwoPass_Stream_Ser` is baseline
  (surfaces BufferWriter-vs-Stream sink delta inside TwoPass).

---

## Reading `[Params]` output

Multi-param benches group rows by the parameter value. Example:

```
| Method   | N    | Mean      | Bytes |
|----------|------|----------:|------:|
| ProtobufNet_De | 0    | 10.4 us | 0     |
| GProtobuf_De   | 0    |  2.6 us | 0     |
|                |      |         |       |
| ProtobufNet_De | 1000 | 42.5 us | 4003  |
| GProtobuf_De   | 1000 | 40.9 us | 4003  |
```

`Bytes` column varies per-Params automatically — crucial for reading
scaling curves.

---

## Gap report (`tools/bench-gaps.ps1`)

BDN writes one CSV per benchmark class to
`BenchmarkDotNet.Artifacts/results/`. That's a lot of numbers to scan by
eye, so the repo ships a post-processing script that distills them into a
single markdown document focused on the OnePass-vs-the-rest question.

### Workflow

```powershell
# 1. Run the benchmarks (any profile — script reads whatever CSVs are present).
dotnet run --project GProtobuf.Benchmark -c Release

# 2. Generate the gap report.
powershell.exe -NoProfile -File tools/bench-gaps.ps1

# Tuning the threshold (default = 1.5x):
powershell.exe -NoProfile -File tools/bench-gaps.ps1 -Threshold 2.0
powershell.exe -NoProfile -File tools/bench-gaps.ps1 -Threshold 1.2 -Output BENCHMARK_GAPS_LOOSE.md
```

Output file defaults to `BENCHMARK_GAPS.md` at repo root. The script is
idempotent — re-running overwrites the file with the current snapshot of
the results directory.

### What's in the document

1. **Section 1 — OnePass slower than TwoPass_Stream.** Primary signal for
   locating OnePass performance gaps. Both methods share the same Stream
   sink, so the ratio isolates OnePass-specific overhead (temp-buffer
   spill, BufferChainPool rent, per-level nesting cost).
2. **Section 2 — OnePass slower than protobuf-net.** Cross-engine
   regressions. Useful for framing "we're still losing to the reference
   implementation here" cases that aren't explained by an internal
   TwoPass gap.
3. **Section 3 — TwoPass slower than protobuf-net.** Separate signal:
   these are TwoPass-path issues that OnePass optimizations won't fix.
   Worth tracking because TwoPass remains the default until OnePass flips
   over.
4. **Glossary.** One entry per benchmark class referenced in the sections
   above, pulled automatically from the `#### ClassName` paragraphs in
   this file. Single source of truth — adding a new bench with its
   description here makes it appear in future gap reports with no script
   change.

Each table row links to the glossary anchor, so clicking the benchmark
name in VS Code / GitHub takes you straight to "what does this
bench measure?" without hunting.

### Tuning guidance

- **`-Threshold 1.5`** (default) surfaces meaningful gaps without flooding
  the report with dev-profile noise.
- **`-Threshold 1.2`** for a close look before/after an optimization — many
  rows, but you're specifically hunting small wins/regressions.
- **`-Threshold 2.0`** for prioritization — only the biggest gaps, good for
  standup-style "what's next" decisions.

The script runs on Windows PowerShell 5.1 (no `pwsh` install required).
It reads BDN CSVs in UTF-8 and writes UTF-8-BOM output so Greek `μ`,
em-dashes, and arrows survive round-tripping.

---

## Adding a new benchmark

### 1. Decide the shape

- **One-axis parameter** (size, shape, depth, count) → `[Params(...)]`
- **No axis** (fixed scenario) → no Params, single-row per method.

### 2. Model

In `Models/{Category}/{Name}Model.cs`. Apply **dual attributes**:

```csharp
using DataFormat = GProtobuf.DataFormat;  // only if you use DataFormat values

namespace GProtobuf.Benchmark.Models.{Category}
{
    [ProtoContract]
    [ProtoBuf.ProtoContract]
    public class FooModel
    {
        [ProtoMember(1)] [ProtoBuf.ProtoMember(1)] public int Value { get; set; }
    }
}
```

**Why dual attributes:** because the benchmark project is under the
`GProtobuf.*` namespace, C# name lookup resolves plain `[ProtoContract]`
to `GProtobuf.ProtoContractAttribute` (from Core) and `using ProtoBuf;`
is never consulted. Explicit `[ProtoBuf.ProtoContract]` is required
so protobuf-net also recognises the type.

**Exception:** `[ProtoInclude]` — apply ONLY the GProtobuf one (see R-009).

### 3. Benchmark

Derive `SerdeBenchmarkBase<T>`, add `[BenchmarkCategory("CiFast")]`, override
`BuildModel` and `PreSerialize`. Copy the standard method set from any
existing bench.

Keep each bench focused on **one axis** — add another class rather than
multi-axis Params.

### 4. Payload size in Bytes column

Automatic. The column reflects over the type at summary time and reads
`Serialized`. No extra wiring required.

### 5. Protobuf-net baseline

Default: `[Benchmark(Baseline = true)]` on the `ProtobufNet_*` method.

Exceptions:
- Polymorphic models (no dual `ProtoInclude`) → baseline should be
  `GProtobuf_TwoPass_Stream_Ser` instead, and a comment explaining why.
- Invariance scans (like `TempBufferSizeBenchmark`) → baseline is the
  variant that actually depends on the param; others are reference rows.

### 6. Smoke-test before merging

```powershell
dotnet run --project GProtobuf.Benchmark -c Release -- --filter "*{YourClass}*" --profile dev
```

Confirm:
- Build is green.
- `Bytes` column shows non-`-` values for every row.
- No `NA` in GProtobuf rows (protobuf-net NA is sometimes expected — document).

---

## Known limitations

See [`BENCHMARK_REGRESSIONS.md`](../BENCHMARK_REGRESSIONS.md) for the
observability backlog. Summary of what affects the suite architecture:

- **R-005** — `List<TEnum>` code-gen fails in `GProtobuf.*` sub-namespaces.
  Blocks enum edge-cases benches until the generator is fixed.
- **R-007** — OnePass nesting depth hard cap of 16 (`[InlineArray(16)]`
  on `NestingStack`). DeepNested Depth=25 rows are NA by design.
- **R-009** — Dual `[ProtoInclude]` triggers duplicate switch-case codegen.
  Inheritance bench's protobuf-net rows are reference-only, not a fair
  baseline.
- **Dev profile noise** — Warmup=1 / Iters=3 / InProcess toolchain. Don't
  chase µs-level differences in dev numbers; switch to `--profile ci-fast`
  or `full` when investigating a regression.

---

## Session history

- **Session 1-2** (Phase 0-2) — Infrastructure + migrated 4 existing benches
- **Session 3** (Phase 3a/b) — Int32 trio + String edge cases
- **Session 4** (Phase 3c) — Bytes edge cases + BENCHMARK_REGRESSIONS.md
- **Session 5** (Phase 4a/5a/5b) — PackedInt32 scaling, DeepNested, EmptyMessage
- **Session 6** (Phase 4b/5c) — Dictionary scaling, Inheritance
- **Session 7** (Phase 6) — OnePassSpecific (TempBufferSize, NestingOverhead)
- **Session 8** — OnePass/TwoPass coverage rebalance: main serialize-set
  reduced to 3 rows (protobuf-net / OnePass / TwoPass_Stream, all same sink);
  BufferWriter sink extracted into new `TwoPassSpecific/` folder

Still pending: Phase 7 (micro-benchmarks), Phase 10 (CI scripts and README).
