namespace Arbor.X.Core
{
    public class ToolResultType
    {
        readonly bool? _succeeded;
        readonly string _type;

        ToolResultType(string type, bool? succeeded)
        {
            _type = type;
            _succeeded = succeeded;
        }

        public string Type => _type;

        public bool IsSuccess => _succeeded.HasValue && _succeeded.Value;

        public bool WasRun => _succeeded.HasValue;

        public static ToolResultType Succeeded => new ToolResultType("Succeeded", true);

        public static ToolResultType Failed => new ToolResultType("Failed", false);

        public static ToolResultType NotRun => new ToolResultType("Not run", null);
    }
}
