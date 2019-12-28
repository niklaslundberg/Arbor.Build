using System;

namespace Arbor.Build.Core
{
    public class TimeService : ITimeService
    {
        public DateTimeOffset UtcNow() => DateTimeOffset.UtcNow;
    }
}