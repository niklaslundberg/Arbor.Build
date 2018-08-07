using System;

namespace Arbor.X.Tests.DummyConsoleApp
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Hello world " + string.Join(", ", args ?? new string[] { }));
        }
    }
}
