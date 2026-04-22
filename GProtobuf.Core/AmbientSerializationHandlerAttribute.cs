using System;

namespace GProtobuf
{
    /// <summary>Wraps every entry-point whose graph reaches <see cref="MarkerType"/> with <see cref="HandlerType"/>'s Before/After hooks.</summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public sealed class AmbientSerializationHandlerAttribute : Attribute
    {
        public AmbientSerializationHandlerAttribute(Type markerType, Type handlerType)
        {
            MarkerType = markerType;
            HandlerType = handlerType;
        }

        /// <summary>Type whose presence in an entry-point's graph triggers wrapping.</summary>
        public Type MarkerType { get; }

        /// <summary>Static class hosting the Before/After hooks.</summary>
        public Type HandlerType { get; }

        /// <summary>Name of the hook called on entry (default <c>"Before"</c>).</summary>
        public string BeforeMethod { get; set; } = "Before";

        /// <summary>Name of the hook called in <c>finally</c>; explicit <c>null</c> opts out.</summary>
        public string AfterMethod { get; set; } = "After";

        /// <summary>Emits a depth counter so hooks fire only at the outermost call.</summary>
        public bool AutoReentrancyGuard { get; set; } = false;

        /// <summary>Wrap serialize entry-points.</summary>
        public bool IncludeSerialization { get; set; } = true;

        /// <summary>Wrap deserialize entry-points.</summary>
        public bool IncludeDeserialization { get; set; } = true;
    }
}
