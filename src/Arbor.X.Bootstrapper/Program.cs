using System;

namespace Arbor.X.Bootstrapper
{
    internal class Program
    {
        static int Main(string[] args)
        {
            try
            {
                var startTask = new Bootstrapper().StartAsync(args);

                var exitCode = startTask.Result.Result;

                return exitCode;
            }
            catch (AggregateException ex)
            {
                Console.Error.WriteLine(ex);

                foreach (var innerEx in ex.InnerExceptions)
                {
                    Console.Error.WriteLine(innerEx);
                }
                return 1;
            }
        }
    }
}