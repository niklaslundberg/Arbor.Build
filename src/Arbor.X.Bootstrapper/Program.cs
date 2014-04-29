using System;
using System.Diagnostics;
using Arbor.X.Core;

namespace Arbor.X.Bootstrapper
{
    internal class Program
    {
        static int Main(string[] args)
        {
            ExitCode exitCode;

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                var startTask = new Bootstrapper().StartAsync(args);

                exitCode = startTask.Result;
            }
            catch (AggregateException ex)
            {
                Console.Error.WriteLine(ex);

                foreach (var innerEx in ex.InnerExceptions)
                {
                    Console.Error.WriteLine(innerEx);
                }
                exitCode = ExitCode.Failure;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                exitCode = ExitCode.Failure;
            }
            finally
            {
                stopwatch.Stop();
            }

            Console.WriteLine("Arbor.X.Bootstrapper total inclusive Arbor.X.Build elapsed time in seconds: {0}", stopwatch.Elapsed.TotalSeconds.ToString("F"));

            return exitCode.Result;
        }
    }
}