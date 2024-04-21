using System;
using Arbor.Build.Core.Time;

namespace Arbor.Build.Tests.Unit;

public class TestTimeService(DateTime dateTime) : ITimeService
{
    public DateTimeOffset UtcNow() => new(dateTime, TimeSpan.Zero);
}