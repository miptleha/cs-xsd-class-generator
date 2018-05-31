using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassGenerator.Generator
{
    class XsdContentReaderOptions
    {
        List<XsdFileInfo> _files = new List<XsdFileInfo>();

        public string CSharpNamespace { get; set; }
        public List<XsdFileInfo> Files { get { return _files; } }
    }

    class XsdFileInfo
    {
        public string FileName { get; set; }
        public string ShortNamespace { get; set; }
    }
}
