using System;
using System.Collections.Generic;
using System.Diagnostics;
using Arbor.X.Core;
using Arbor.X.Core.Logging;

namespace Arbor.X.Build
{
    internal class Program
    {
        static BuildApplication _app;

        static string GetFormat(ExitCode exitCode, IEnumerable<string> args)
        {
            const string prefix = "[Arbor.X.Build] ";
            var format = prefix + string.Format("Exit code={0}, instance hash code ({1}), arguments: {2}", exitCode,
                                                _app.GetHashCode(), string.Join(" ", args));

            return format;
        }

        static int Main(string[] args)
        {
            try
            {
                _app = new BuildApplication(new ConsoleLogger());
                ExitCode exitCode = _app.RunAsync().Result;

                var format = GetFormat(exitCode, args);
                Console.WriteLine(format);
                Debug.WriteLine(format);
                return exitCode.Result;
            }
            catch (AggregateException ex)
            {
                Console.Error.WriteLine(ex);

                foreach (var innerException in ex.InnerExceptions)
                {
                    var format = GetFormat(ExitCode.Failure, args);
                    Console.Error.WriteLine("{0}{1}{2}", format, Environment.NewLine, innerException.InnerException);
                }

                return ExitCode.Failure.Result;
            }
            catch (Exception ex)
            {
                var format = GetFormat(ExitCode.Failure, args);
                Console.Error.WriteLine("{0}{1}{2}", format, Environment.NewLine, ex);
                return ExitCode.Failure.Result;
            }
        }
    }
}