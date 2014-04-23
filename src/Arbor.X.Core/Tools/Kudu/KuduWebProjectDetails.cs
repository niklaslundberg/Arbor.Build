using System;

namespace Arbor.X.Core.Tools.Kudu
{
    public class KuduWebProjectDetails
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
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("name");
            }

            if (string.IsNullOrWhiteSpace(type))
            {
                throw new ArgumentNullException("type");
            }

            if (string.IsNullOrWhiteSpace(projectFilePath))
            {
                throw new ArgumentNullException("projectFilePath");
            }

            var webJobProject = new KuduWebProjectDetails(true, ParseName(name), KuduWebJobType.Parse(type), projectFilePath);

            return webJobProject;
        }

        static string ParseName(string name)
        {
            return name.ExtractFromTag("KuduWebJobName");
        }

        public static KuduWebProjectDetails NotAKuduWebJobProject()
        {
            return new KuduWebProjectDetails(false);
        }

        public override string ToString()
        {
            return IsKuduWebJobProject
                ? string.Format("{0} ({1}), path '{2}'", WebJobName, KuduWebJobType, ProjectFilePath)
                : "Not a Kudu web project";
        }
    }
}