using System;

namespace Arbor.X.Core.Logging
{
    public class ConsoleLogger : ILogger
    {
        public void WriteError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(message);
            Console.ResetColor();
        }

        public void Write(string message)
        {
            Console.ResetColor();
            Console.WriteLine(message);
        }


        public void Write(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        public void WriteWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        public void WriteVerbose(string message)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}