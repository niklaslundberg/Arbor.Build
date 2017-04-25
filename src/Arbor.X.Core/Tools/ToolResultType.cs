namespace Arbor.X.Core.Tools
{
    public class ToolResultType
    {
        private readonly bool? _succeeded;

        private ToolResultType(string type, bool? succeeded)
        {
            Type = type;
            _succeeded = succeeded;
        }

        public string Type { get; }

        public bool IsSuccess => _succeeded.HasValue && _succeeded.Value;

        public bool WasRun => _succeeded.HasValue;

        public static ToolResultType Succeeded => new ToolResultType("Succeeded", true);

        public static ToolResultType Failed => new ToolResultType("Failed", false);

        public static ToolResultType NotRun => new ToolResultType("Not run", null);
    }
}
