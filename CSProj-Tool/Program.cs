using System.Xml;

namespace csproj.tool
{
    class Program
    {
        static void Main(string[] args)
        {
            var xml = new XmlDocument();
            xml.Load(args[0]);


        }
    }
}
