using ClassGenerator.AF;
using System.Xml.Linq;

namespace ClassGenerator.Xml
{
    interface IXml
    {
        void Init(XElement r);
        XElement ToXElement(XName name, Namespaces ns);
    }
}
