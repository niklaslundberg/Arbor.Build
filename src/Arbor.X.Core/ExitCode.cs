namespace Arbor.X.Core
{
    public sealed class ExitCode
    {
        readonly int _result;

        public ExitCode(int result)
        {
            _result = result;
        }

        public bool IsSuccess => _result == 0;

        public static ExitCode Success => new ExitCode(0);

        public static ExitCode Failure => new ExitCode(1);

        public int Result => _result;

        public override string ToString()
        {
            string successOrFailure = IsSuccess ? "Success" : "Failure";

            return $"[{Result}, {successOrFailure}]";
        }
    }
}
