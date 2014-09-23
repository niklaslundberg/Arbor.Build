namespace Arbor.X.Core.BuildVariables
{
    internal class ArborX
    {
        const string Namespace = "Arbor.X";

        public ArborX()
        {
        }

        public static implicit operator string(ArborX arborX)
        {
            return Namespace;
        }

        public string Build
        {
            get { return Namespace + ".Build"; }
        }
    }
}