using System;
using System.Collections.Generic;
using System.Diagnostics;
using Arbor.X.Core;

namespace Arbor.X.Build
{
    internal class Program
    {
        static BuildApplication _app;

        static string GetFormat(int exitCode, IEnumerable<string> args)
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
                _app = new BuildApplication();
                int exitCode = _app.RunAsync().Result;

                var format = GetFormat(exitCode, args);
                Console.WriteLine(format);
                Debug.WriteLine(format);
                return exitCode;
            }
            catch (AggregateException ex)
            {
                const int exitCode = 1;
                Console.Error.WriteLine(ex);

                foreach (var innerException in ex.InnerExceptions)
                {
                    var format = GetFormat(exitCode, args);
                    Console.Error.WriteLine(format + Environment.NewLine + innerException.InnerException);
                }

                return exitCode;
            }
            catch (Exception ex)
            {
                const int exitCode = 1;
                var format = GetFormat(exitCode, args);
                Console.Error.WriteLine(format + Environment.NewLine + ex.InnerException);
                return exitCode;
            }
        }
    }
}