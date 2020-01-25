using System;

namespace Arbor.Build.Core
{
    public interface ITimeService
    {
        DateTimeOffset UtcNow();
    }
}