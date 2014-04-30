namespace Arbor.X.Core
{
    public class ToolResultType
    {
        public string Type
        {
            get { return _type; }
        }

        readonly string _type;
        readonly bool? _succeeded;

        ToolResultType(string type, bool? succeeded)
        {
            _type = type;
            _succeeded = succeeded;
        }

        public bool IsSuccess {get { return _succeeded.HasValue && _succeeded.Value; }}

        public bool WasRun {get { return _succeeded.HasValue; }}

        public static ToolResultType Succeeded
        {
            get { return new ToolResultType("Succeeded", true); }
        }

        public static ToolResultType Failed
        {
            get { return new ToolResultType("Failed", false); }
        }

        public static ToolResultType NotRun
        {
            get { return new ToolResultType("Not run", null); }
        }
    }
}