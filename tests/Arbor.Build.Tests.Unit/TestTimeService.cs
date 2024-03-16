using System;
using Arbor.Build.Core;

namespace Arbor.Build.Tests.Unit;

public class TestTimeService(DateTime dateTime) : ITimeService
{
    public DateTimeOffset UtcNow() => new(dateTime, TimeSpan.Zero);
}