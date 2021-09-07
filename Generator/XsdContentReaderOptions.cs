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
        /// Add methods to read data from xml and store into xml
        /// </summary>
        public bool IsXml { get; set; }

        /// <summary>
        /// Add methods to generate DB and futher generation for inserts (QueryGenerator project)
        /// </summary>
        public bool StoreDB { get; set; }

        /// <summary>
        /// Add methods to read data from DB
        /// </summary>
        public bool ReadDB { get; set; }
        
        /// <summary>
        /// Tables prefixes in DB
        /// </summary>
        public string StoreDBPrefix { get; set; }

        /// <summary>
        /// Fields in DB with names as properties in classes
        /// </summary>
        public bool ExactDBNames { get; set; }

        /// <summary>
        /// Set of schemas
        /// </summary>
        public List<XsdFileInfo> Files { get { return _files; } }

        /// <summary>
        /// Parse one file at a time, not as single set
        /// </summary>
        public bool FileByFile { get; set; }

        /// <summary>
        /// Translator for names
        /// </summary>
        public Dictionary<string, string> Translator { get; set; }
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

        /// <summary>
        /// Prefix for all classes
        /// </summary>
        public string Prefix { get; set; }
    }
}
