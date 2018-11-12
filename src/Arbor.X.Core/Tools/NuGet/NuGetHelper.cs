using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.GenericExtensions.Boolean;
using Arbor.Build.Core.ProcessUtils;
using Serilog;

namespace Arbor.Build.Core.Tools.NuGet
{
    public class NuGetHelper
    {
        private readonly ILogger _logger;

        public NuGetHelper(ILogger logger)
        {
            _logger = logger;
        }

    }
}
