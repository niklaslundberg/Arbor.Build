using System;

namespace Arbor.Build.Core.Time;

public class TimeService : ITimeService
{
    public DateTimeOffset UtcNow() => DateTimeOffset.UtcNow;
}