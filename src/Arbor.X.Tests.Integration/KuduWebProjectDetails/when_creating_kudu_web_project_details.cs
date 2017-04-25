using System;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.KuduWebProjectDetails
{
    [Subject(typeof(Core.Tools.Kudu.KuduWebProjectDetails))]
    public class when_creating_kudu_web_project_details
    {
        private static Core.Tools.Kudu.KuduWebProjectDetails parsed;

        private Because of = () =>
        {
            parsed = Core.Tools.Kudu.KuduWebProjectDetails.Create("<KuduWebJobName>MyWebJob</KuduWebJobName>",
                "<KuduWebJobType>Continuous</KuduWebJobType>", @"C:\Temp\test.csproj");
        };

        private It should_return_a_valid_type = () =>
        {
            Console.WriteLine(parsed);
            parsed.ShouldNotBeNull();
        };
    }
}
