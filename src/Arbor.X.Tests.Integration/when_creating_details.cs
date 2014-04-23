using System;
using Arbor.X.Core.Tools.Kudu;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration
{
    [Subject(typeof(KuduWebProjectDetails))]
    public class when_creating_details
    {
        static KuduWebProjectDetails parsed;

        Because of = () => { parsed = KuduWebProjectDetails.Create("<KuduWebJobName>MyWebJob</KuduWebJobName>", "<KuduWebJobType>Continuous</KuduWebJobType>", @"C:\Temp\test.csproj"); };

        It should_return_a_valid_type = () =>
        {
            Console.WriteLine(parsed);
            parsed.ShouldNotBeNull();
        };
    }
}