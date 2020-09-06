using System;

namespace Arbor.Build.Core.Tools
{
    public class ToolResultType : IEquatable<ToolResultType>
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

        public bool Equals(ToolResultType? other)
        {
            if (ReferenceEquals(null, other))
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

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((ToolResultType)obj);
        }

        public override int GetHashCode() => Type.GetHashCode(StringComparison.Ordinal);

        public static bool operator ==(ToolResultType? left, ToolResultType? right) => Equals(left, right);

        public static bool operator !=(ToolResultType? left, ToolResultType? right) => !Equals(left, right);
    }
}