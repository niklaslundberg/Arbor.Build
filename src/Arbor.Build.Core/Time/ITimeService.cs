using System;

namespace Arbor.Build.Core.Time;

public interface ITimeService
{
    DateTimeOffset UtcNow();
}