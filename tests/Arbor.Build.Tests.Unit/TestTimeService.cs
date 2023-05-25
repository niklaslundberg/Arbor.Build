using System;
using Arbor.Build.Core;

namespace Arbor.Build.Tests.Unit;

public class TestTimeService : ITimeService
{
    private readonly DateTime _dateTime;

    public TestTimeService(DateTime dateTime) => _dateTime = dateTime;

    public DateTimeOffset UtcNow() => new DateTimeOffset(_dateTime, TimeSpan.Zero);
}