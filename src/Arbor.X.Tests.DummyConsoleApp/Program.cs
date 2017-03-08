using System;

namespace Arbor.X.Tests.DummyConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello world " + string.Join(", ", args ?? new string [] {}));
        }
    }
}
