using System;
using Arbor.X.Core.Tools;

namespace Arbor.X.Core
{
    public class ToolResult
    {
        public ToolWithPriority ToolWithPriority
        {
            get { return _toolWithPriority; }
        }

        public ToolResultType ResultType
        {
            get { return _resultType; }
        }

        public string Message
        {
            get { return _message; }
        }

        public TimeSpan ExecutionTime
        {
            get { return _executionTime; }
        }

        readonly ToolWithPriority _toolWithPriority;
        readonly ToolResultType _resultType;
        readonly string _message;
        readonly TimeSpan _executionTime;

        public ToolResult(ToolWithPriority toolWithPriority, ToolResultType resultType, string message = null, TimeSpan executionTime = default(TimeSpan))
        {
            _toolWithPriority = toolWithPriority;
            _resultType = resultType;
            _message = message;
            _executionTime = executionTime;
        }
    }
}