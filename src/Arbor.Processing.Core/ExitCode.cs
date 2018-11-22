using System;

namespace Arbor.Processing.Core
{
    public sealed class ExitCode : IEquatable<ExitCode>
    {
        public ExitCode(int result)
        {
            Result = result;
        }

        public int Result { get; }

        public bool IsSuccess => Result == 0;

        public static ExitCode Success => new ExitCode(0);

        public static ExitCode Failure => new ExitCode(1);

        public override string ToString()
        {
            string successOrFailure = IsSuccess ? "Success" : "Failure";

            return $"[{Result}, {successOrFailure}]";
        }

        public bool Equals(ExitCode other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Result == other.Result;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj is ExitCode other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Result;
        }

        public static bool operator ==(ExitCode left, ExitCode right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ExitCode left, ExitCode right)
        {
            return !Equals(left, right);
        }
    }
}
