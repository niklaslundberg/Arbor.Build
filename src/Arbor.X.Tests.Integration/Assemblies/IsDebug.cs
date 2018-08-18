using System;
using System.Diagnostics;
using System.Reflection;
using Arbor.Build.Core.Assemblies;
using Serilog.Core;
using Xunit;

namespace Arbor.Build.Tests.Integration.Assemblies
{
    #if DEBUG
    public class IsDebug
    {
        [Fact]
        public void ShouldBeDebug()
        {
            bool? isDebugAssembly = typeof(IsDebug).Assembly.IsDebugAssembly(Logger.None);

            Assert.True(isDebugAssembly);
        }

        [Fact]
        public void ShouldBeDebugWithReflectionOnly()
        {
            Assembly reflectionOnlyLoadedAssembly;
            try
            {
                reflectionOnlyLoadedAssembly = Assembly.ReflectionOnlyLoad(typeof(IsDebug).Assembly.FullName);
            }
            catch (PlatformNotSupportedException)
            {
                reflectionOnlyLoadedAssembly = null;
            }
            catch (NotSupportedException)
            {
                reflectionOnlyLoadedAssembly = null;
            }

            if (reflectionOnlyLoadedAssembly != null)
            {
                bool? isDebugAssembly = reflectionOnlyLoadedAssembly.IsDebugAssembly(Logger.None);
                Assert.True(isDebugAssembly);
            };
        }


    }
    #else
    public class IsDebug
    {
        [Fact]
        public void ShouldBeNonDebug()
        {
            bool? isDebugAssembly = typeof(IsDebug).Assembly.IsDebugAssembly(Logger.None);

            Assert.False(isDebugAssembly);
        }
    }
    #endif
}
