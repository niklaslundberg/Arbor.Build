namespace Arbor.Processing.Core
{
    public sealed class ExitCode
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
    }
}
