using System;
using Arbor.X.Core.Tools;

namespace Arbor.X.Core
{
    public class ToolResult
    {
        readonly TimeSpan _executionTime;
        readonly string _message;
        readonly ToolResultType _resultType;
        readonly ToolWithPriority _toolWithPriority;

        public ToolResult(ToolWithPriority toolWithPriority, ToolResultType resultType, string message = null,
            TimeSpan executionTime = default(TimeSpan))
        {
            _toolWithPriority = toolWithPriority;
            _resultType = resultType;
            _message = message;
            _executionTime = executionTime;
        }

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
    }
}