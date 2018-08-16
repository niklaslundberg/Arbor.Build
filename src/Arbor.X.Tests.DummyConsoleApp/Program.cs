using System;

namespace Arbor.Build.Tests.DummyConsoleApp
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Hello world " + string.Join(", ", args ?? new string[] { }));
        }
    }
}
