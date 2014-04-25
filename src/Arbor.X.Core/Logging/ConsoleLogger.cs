using System;

namespace Arbor.X.Core.Logging
{
    public class ConsoleLogger : ILogger
    {
        readonly string _prefix;

        public ConsoleLogger(string prefix = "")
        {
            _prefix = prefix;
        }

        public void WriteError(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(_prefix + message);
            Console.ResetColor();
        }

        public void Write(string message)
        {
            Console.ResetColor();
            Console.WriteLine(_prefix + message);
        }


        public void Write(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(_prefix + message);
            Console.ResetColor();
        }

        public void WriteWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(_prefix + message);
            Console.ResetColor();
        }

        public void WriteVerbose(string message)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(_prefix + message);
            Console.ResetColor();
        }
    }
}