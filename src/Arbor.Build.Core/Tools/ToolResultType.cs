using System;

namespace Arbor.Build.Core.Tools
{
    public sealed class ToolResultType : IEquatable<ToolResultType>
    {
        private readonly bool? _succeeded;

        private ToolResultType(string type, bool? succeeded)
        {
            Type = type;
            _succeeded = succeeded;
        }

        public string Type { get; }

        public bool IsSuccess => _succeeded.HasValue && _succeeded.Value;

        public bool WasRun => _succeeded.HasValue;

        public static ToolResultType Succeeded => new ToolResultType("Succeeded", true);

        public static ToolResultType Failed => new ToolResultType("Failed", false);

        public static ToolResultType NotRun => new ToolResultType("Not run", null);

        public override bool Equals(object? obj) => Equals(obj as ToolResultType);

        public bool Equals(ToolResultType? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Type == other.Type;
        }

        public override string ToString() => $"{nameof(Type)}: {Type}";

        public override int GetHashCode() => Type.GetHashCode(StringComparison.Ordinal);

        public static bool operator ==(ToolResultType? left, ToolResultType? right) => Equals(left, right);

        public static bool operator !=(ToolResultType? left, ToolResultType? right) => !Equals(left, right);
    }
}