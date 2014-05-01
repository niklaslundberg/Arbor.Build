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

        public void WriteError(string message, string prefix = null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }
            
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(GetPrefix(prefix) + message);
            Console.ResetColor();
        }

        object GetPrefix(string prefix)
        {
            string value;

            if (!string.IsNullOrWhiteSpace(prefix))
            {
                value = prefix;
            }
            else
            {
                value = _prefix;
            }

            if (!value.EndsWith(" "))
            {
                value = value + " ";
            }
            return value;
        }

        public void Write(string message, string prefix = null)
        {
            Console.ResetColor();
            Console.WriteLine(GetPrefix(prefix) + message);
        }


        public void Write(string message, ConsoleColor color, string prefix = null)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(GetPrefix(prefix) + message);
            Console.ResetColor();
        }

        public void WriteWarning(string message, string prefix = null)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(GetPrefix(prefix) + message);
            Console.ResetColor();
        }

        public void WriteVerbose(string message, string prefix = null)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(GetPrefix(prefix) + message);
            Console.ResetColor();
        }
    }
}