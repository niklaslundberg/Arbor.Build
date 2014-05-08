using System;
using Arbor.X.Core.Logging;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Logging
{
    [Subject(typeof (ConsoleLogger))]
    public class when_Specification
    {
        static ConsoleLogger logger;
        static string prefix1;
        static string prefix2;

        Establish context = () =>
        {
            prefix1 = "TEST";
            prefix2 = "TEST2 ";
            logger = new ConsoleLogger(prefix1);
        };

        Because of = () =>
        {
            logger.Write("A");
            logger.Write(Environment.NewLine + "B");
            logger.Write(Environment.NewLine + Environment.NewLine + "C" + Environment.NewLine);
            logger.Write("D");

            logger.Write("E", prefix2);
            logger.Write(Environment.NewLine + "F", prefix2);
            logger.Write(Environment.NewLine + Environment.NewLine + "G" + Environment.NewLine, prefix2);
            logger.Write(" H", prefix2);
        };

        It should_Behaviour = () => { };
    }
}