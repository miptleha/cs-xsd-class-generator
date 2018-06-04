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

        /// <summary>
        /// Namespace in generated files
        /// </summary>
        public string CSharpNamespace { get; set; }

        /// <summary>
        /// Add methods to work with DB
        /// </summary>
        public bool StoreDB { get; set; }

        /// <summary>
        /// Tables prefixes in DB
        /// </summary>
        public string StoreDBPrefix { get; set; }

        /// <summary>
        /// Set of schemas
        /// </summary>
        public List<XsdFileInfo> Files { get { return _files; } }
    }

    class XsdFileInfo
    {
        /// <summary>
        /// Path to the schema relative to exe file
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Name of XNamespace property for schema (in Namespaces.cs)
        /// </summary>
        public string ShortNamespace { get; set; }
    }
}
