using Xml;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using QueryGenerator;
using Db;
using System.Data.Common;
using Misc;
using ClassGenerator.AF;

namespace SampleService.AF.Kps
{
    //this is generated file from bin/debug/code folder
    public class RootType : IXml, IRow, IQObject
    {
        private List<string> _empty = new List<string>();
        private StringWithAttrType _StringWithAttr = new StringWithAttrType();

        public List<string> empty { get { return _empty; } } //optional, 
        public string StringElement1 { get; set; } //maxLen: 20, 
        public string StringElement2 { get; set; } //
        public string StringElement3 { get; set; } //pattern: [A-Z]*, 
        public StringWithAttrType StringWithAttr { get { return _StringWithAttr; } } //

        public void Init(DbDataReader r, Dictionary<string, int> columns)
        {
            StringElement1 = Util.ToStr(r["StringElement1"]);
            StringElement2 = Util.ToStr(r["StringElement2"]);
            StringElement3 = Util.ToStr(r["StringElement3"]);
            StringWithAttr.Init(r, columns);
        }

        public void Init(XElement r)
        {
            empty.Clear();
            var list = XmlParser.Elements(r, "empty");
            foreach (var i in list)
                empty.Add(i.Value);

            StringElement1 = XmlParser.ElementValue(r, "StringElement1");
            StringElement2 = XmlParser.ElementValue(r, "StringElement2");
            StringElement3 = XmlParser.ElementValue(r, "StringElement3");
            StringWithAttr.Init(XmlParser.Element(r, "StringWithAttr"));
        }

        public XElement ToXElement(XName name, Namespaces ns)
        {
            var r = new XElement(name);

            foreach (var i in empty)
                r.Add(new XElement(ns.Test + "empty", i));
            r.Add(new XElement(ns.Test + "StringElement1", StringElement1));
            r.Add(new XElement(ns.Test + "StringElement2", StringElement2));
            r.Add(new XElement(ns.Test + "StringElement3", StringElement3));
            r.Add(StringWithAttr.ToXElement(ns.Test + "StringWithAttr", ns));

            return r;
        }

        public void StoreInfo(QData data)
        {
            var qt = new QTable { Name = "a_Tes", Comment = "", Pk = "Id", PkComment = "Идентификатор" };
            RootType.StoreInfo(qt, null, "", null, "a_Tes_", null, data);
        }

        internal static void StoreInfo(QTable qt, QHierarchy h, string prefix, string comment, string tab_prefix, string tab_comment, QData data)
        {
            var prf = (prefix != null && prefix.Length > 10 ? prefix.Substring(0, 5) + prefix.Substring(prefix.Length - 5) : prefix);
            data.AddInfo(qt, h,
                new QField { Name = "StringElement1", Type = QType.String, Size = 20, Prefix = prf, Comment = (comment != null ? comment + ": " : "") + "" },
                new QField { Name = "StringElement2", Type = QType.String, Size = 100, Prefix = prf, Comment = (comment != null ? comment + ": " : "") + "" },
                new QField { Name = "StringElement3", Type = QType.String, Size = 100, Prefix = prf, Comment = (comment != null ? comment + ": " : "") + "" });

            StringList.StoreInfo(new QTable { Name = (tab_prefix.Length > 23 ? tab_prefix.Substring(0, 23) : tab_prefix) + "emp", Comment = (tab_comment != null ? tab_comment + ": " : "") + "", Pk = "Id", PkComment = "Идентификатор", Fk = "IdFk", FkComment = "Идентификатор родительской записи" },
                new QHierarchy("empty", QHType.List, h), -1, "", data);

            StringWithAttrType.StoreInfo(qt, new QHierarchy("StringWithAttr", QHType.Member, h), prefix + "SWA", (comment != null ? comment + ": " : "") + "", tab_prefix + "SWA", (tab_comment != null ? tab_comment + ": " : "") + "", data);
        }
    }

    //
    public class StringWithAttrType : IXml, IRow
    {
        public void Init(DbDataReader r, Dictionary<string, int> columns)
        {
        }

        public void Init(XElement r)
        {
        }

        public XElement ToXElement(XName name, Namespaces ns)
        {
            var r = new XElement(name);


            return r;
        }

        internal static void StoreInfo(QTable qt, QHierarchy h, string prefix, string comment, string tab_prefix, string tab_comment, QData data)
        {
            data.AddInfo(qt, h);
        }
    }

}
