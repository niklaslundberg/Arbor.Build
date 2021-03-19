using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Arbor.Exceptions
{
    public static class ExceptionExtensions
    {
        public static bool IsFatal(this Exception? ex) =>
            ex switch
            {
                StackOverflowException => true,
                OutOfMemoryException => true,
                AccessViolationException => true,
                AppDomainUnloadedException => true,
                ThreadAbortException => true,
                SEHException => true,
                _ => false
            };
    }
}
