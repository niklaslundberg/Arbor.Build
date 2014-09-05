namespace Arbor.X.Core
{
    public sealed class ExitCode
    {
        readonly int _result;

        public ExitCode(int result)
        {
            _result = result;
        }

        public bool IsSuccess
        {
            get { return _result == 0; }
        }

        public static ExitCode Success
        {
            get { return new ExitCode(0); }
        }

        public static ExitCode Failure
        {
            get { return new ExitCode(1); }
        }

        public int Result
        {
            get { return _result; }
        }

        public override string ToString()
        {
            string successOrFailure = IsSuccess ? "Success" : "Failure";

            return string.Format("[{0}, {1}]", Result, successOrFailure);
        }
    }
}