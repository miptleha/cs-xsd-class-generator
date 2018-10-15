Console application that converts xsd to cs classes. Generated classes have methods to serialize/deserialize to xml. Also generates methods for [Database generator](https://github.com/miptleha/cs-query-generator).

## How to use
-   put your xsd schemas to bin\Debug folder (in root or in subfolder)
-   describe them in Program.cs file (see Sample section)
-   if some errors see log\ClassGenerator.log
-   all classes will be generated in code folder inside bin\Debug
-   to work with classes your will need helpers inside AF, Xml folder
-   note: fields generated only for elements, not for attributes

## Sample

There is sample xsd schema (included in project):
```xsd
<?xml version="1.0" encoding="utf-8"?>
<xs:schema id="Sample" targetNamespace="urn:test" elementFormDefault="qualified" xmlns="urn:test" xmlns:xs="http://www.w3.org/2001/XMLSchema">
   <xs:simpleType name="stringRegEx">
      <xs:annotation>
         <xs:documentation>only letters A through z (type documentation)</xs:documentation>
      </xs:annotation>
      <xs:restriction base="xs:string">
         <xs:pattern value="[A-z]*" />
      </xs:restriction>
   </xs:simpleType>
   <xs:simpleType name="string20">
      <xs:annotation>
         <xs:documentation>only letters A through z</xs:documentation>
      </xs:annotation>
      <xs:restriction base="xs:string">
         <xs:maxLength value="20" />
      </xs:restriction>
   </xs:simpleType>
   <xs:complexType name="RootType">
      <xs:sequence>
         <xs:element name="empty" type="xs:string" minOccurs="0"  maxOccurs="unbounded"/>
         <xs:element name="StringElement1" nillable="true" type="string20" />
         <xs:element name="StringElement2" default="ABC" type="xs:string" />
         <xs:element name="StringElement3">
            <xs:simpleType>
               <xs:restriction base="xs:string">
                  <xs:pattern value="[A-Z]*" />
               </xs:restriction>
            </xs:simpleType>
         </xs:element>
         <xs:element name="StringWithAttr">
            <xs:complexType>
               <xs:simpleContent>
                  <xs:extension base="xs:string">
                     <xs:attribute name="attr1" type="xs:integer" />
                     <xs:attribute name="attr2" type="xs:duration">
                        <xs:annotation>
                           <xs:appinfo>assemblyName, DotNetClass</xs:appinfo>
                        </xs:annotation>
                     </xs:attribute>
                     <xs:attribute name="attr3" type="stringRegEx">
                        <xs:annotation>
                           <xs:documentation>declaration documentation</xs:documentation>
                        </xs:annotation>
                     </xs:attribute>
                  </xs:extension>
               </xs:simpleContent>
            </xs:complexType>
         </xs:element>
      </xs:sequence>
   </xs:complexType>
   <xs:element name="Root" type="RootType" />
</xs:schema>
```

put some code in Program.cs:
```cs
//see XsdContentReaderOptions class for more options
var opt = new XsdContentReaderOptions();
opt.StoreDB = true;
opt.StoreDBPrefix = "a";
opt.CSharpNamespace = "SampleService";
opt.Files.Add(new XsdFileInfo { FileName = "sample.xsd", ShortNamespace = "Test" });

var reader = new XsdContentReader();
var content = reader.GenerateClasses(opt);
```

it will output to class:
```cs
using Xml;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using QueryGenerator;

namespace SampleService.AF.Kps
{
    //
    public class RootType : IXml, IQObject
    {
        private List<string> _empty = new List<string>();
        private string _StringWithAttr = new string();

        public List<string> empty { get { return _empty; } } //optional, 
        public string StringElement1 { get; set; } //maxLen: 20, 
        public string StringElement2 { get; set; } //
        public string StringElement3 { get; set; } //pattern: [A-Z]*, 
        public string StringWithAttr { get { return _StringWithAttr; } } //

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
            var qt = new QTable { Name = "a_Tes", Comment = "", Pk = "Id", PkComment = "Id" };
            RootType.StoreInfo(qt, null, "", null, "a_Tes_", null, data);
        }

        internal static void StoreInfo(QTable qt, QHierarchy h, string prefix, string comment, string tab_prefix, string tab_comment, QData data)
        {
            var prf = (prefix != null && prefix.Length > 10 ? prefix.Substring(0, 5) + prefix.Substring(prefix.Length - 5) : prefix);
            data.AddInfo(qt, h,
                new QField { Name = "StringElement1", Type = QType.String, Size = 20, Prefix = prf, Comment = (comment != null ? comment + ": " : "") + "" },
                new QField { Name = "StringElement2", Type = QType.String, Size = 100, Prefix = prf, Comment = (comment != null ? comment + ": " : "") + "" },
                new QField { Name = "StringElement3", Type = QType.String, Size = 100, Prefix = prf, Comment = (comment != null ? comment + ": " : "") + "" });

            string.StoreInfo(qt, new QHierarchy("StringWithAttr", QHType.Member, h), prefix + "SWA", (comment != null ? comment + ": " : "") + "", tab_prefix + "SWA", (tab_comment != null ? tab_comment + ": " : "") + "", data);
        }
    }

    //
    public class StringWithAttrType : , IXml
    {
        public new void Init(XElement r)
        {
            base.Init(r);

        }

        public new XElement ToXElement(XName name, Namespaces ns)
        {
            var r = base.ToXElement(name, ns);


            return r;
        }

        internal static new void StoreInfo(QTable qt, QHierarchy h, string prefix, string comment, string tab_prefix, string tab_comment, QData data)
        {
            data.AddInfo(qt, h);
            .StoreInfo(qt, h, prefix, comment, tab_prefix, tab_comment, data);
        }
    }
}
```
