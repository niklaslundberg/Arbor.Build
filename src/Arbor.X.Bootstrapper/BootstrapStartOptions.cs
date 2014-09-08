namespace Arbor.X.Bootstrapper
{
    public class BootstrapStartOptions
    {
        readonly string _baseDir;
        readonly bool? _prereleaseEnabled;
        readonly string _branchName;

        public BootstrapStartOptions(string baseDir = null, bool? prereleaseEnabled = null, string branchName = null)
        {
            _baseDir = baseDir;
            _prereleaseEnabled = prereleaseEnabled;
            _branchName = branchName;
        }

        public bool? PrereleaseEnabled
        {
            get { return _prereleaseEnabled; }
        }

        public string BaseDir
        {
            get { return _baseDir; }
        }

        public string BranchName
        {
            get { return _branchName; }
        }

        public static BootstrapStartOptions Parse(string[] args)
        {
            return new BootstrapStartOptions();
        }
    }
}