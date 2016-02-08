using System;

namespace Arbor.X.Core
{
    public struct ExitCode
    {
        readonly int _result;

        public ExitCode(int result)
        {
            _result = result;
        }

        public bool IsSuccess => _result == 0;

        private static readonly Lazy<ExitCode> _Success = new Lazy<ExitCode>(() => new ExitCode(0));

        private static readonly Lazy<ExitCode> _Failure = new Lazy<ExitCode>(() => new ExitCode(1));

        public static ExitCode Success => _Success.Value;

        public static ExitCode Failure => _Failure.Value;

        public int Result => _result;

        public override string ToString()
        {
            string successOrFailure = IsSuccess ? "Success" : "Failure";

            return $"[{Result}, {successOrFailure}]";
        }
    }
}
