namespace Arbor.X.Core.Tools.Kudu
{
    internal class KuduWebProjectDetails
    {
        readonly bool _isKuduWebJobProject;
        readonly KuduWebJobType _kuduWebJobType;
        readonly string _projectFilePath;
        readonly string _webJobName;

        KuduWebProjectDetails(bool isKuduWebJobProject, string webJobName = null, KuduWebJobType kuduWebJobType = null,
            string projectFilePath = null)
        {
            _isKuduWebJobProject = isKuduWebJobProject;
            _webJobName = webJobName ?? "";
            _kuduWebJobType = kuduWebJobType;
            _projectFilePath = projectFilePath ?? "";
        }

        public bool IsKuduWebJobProject
        {
            get { return _isKuduWebJobProject; }
        }

        public string WebJobName
        {
            get { return _webJobName; }
        }

        public KuduWebJobType KuduWebJobType
        {
            get { return _kuduWebJobType; }
        }

        public string ProjectFilePath
        {
            get { return _projectFilePath; }
        }

        public static KuduWebProjectDetails Create(string name, string type, string projectFilePath)
        {
            var webJobProject = new KuduWebProjectDetails(true, name, KuduWebJobType.Parse(type), projectFilePath);

            return webJobProject;
        }

        public static KuduWebProjectDetails NotAKuduWebJobProject()
        {
            return new KuduWebProjectDetails(false);
        }
    }
}