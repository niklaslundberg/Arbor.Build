using System;

namespace Arbor.X.Core.Tools.Kudu
{
    public class KuduWebProjectDetails
    {
        private KuduWebProjectDetails(bool isKuduWebJobProject, string webJobName = null,
            KuduWebJobType kuduWebJobType = null,
            string projectFilePath = null)
        {
            IsKuduWebJobProject = isKuduWebJobProject;
            WebJobName = webJobName ?? string.Empty;
            KuduWebJobType = kuduWebJobType;
            ProjectFilePath = projectFilePath ?? string.Empty;
        }

        public bool IsKuduWebJobProject { get; }

        public string WebJobName { get; }

        public KuduWebJobType KuduWebJobType { get; }

        public string ProjectFilePath { get; }

        public static KuduWebProjectDetails Create(string name, string type, string projectFilePath)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (string.IsNullOrWhiteSpace(type))
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (string.IsNullOrWhiteSpace(projectFilePath))
            {
                throw new ArgumentNullException(nameof(projectFilePath));
            }

            var webJobProject = new KuduWebProjectDetails(true, ParseName(name), KuduWebJobType.Parse(type),
                projectFilePath);

            return webJobProject;
        }

        private static string ParseName(string name)
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
                ? $"{WebJobName} ({KuduWebJobType}), path '{ProjectFilePath}'"
                : "Not a Kudu web project";
        }
    }
}
