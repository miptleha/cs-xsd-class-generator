# CSharp class generator for xsd schema

Console application that converts xsd to cs classes.<br/>
Generated classes have methods to serialize/deserialize to xml (optional).<br/>
Generated classes have methods to initialize object from db (optional).<br/>
Generate methods for [Database generator](https://github.com/miptleha/cs-query-generator) (optional).


## How to use
-   put your xsd schemas to bin\Debug folder (in root or in subfolder)
-   describe them in Program.cs file (see Sample section)
-   if some errors see log\ClassGenerator.log
-   all classes will be generated inside bin\Debug\code folder
-   to compile and work with classes your will need helpers inside AF, Xml, QueryGenerator, Db folder
-   note: fields generated only for elements, not for attributes!

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
opt.CSharpNamespace = "SampleService";
opt.Files.Add(new XsdFileInfo { FileName = "sample.xsd", ShortNamespace = "Test" });

var reader = new XsdContentReader();
var content = reader.GenerateClasses(opt);
```

run application, it will output to class:
```cs
using System;
using System.Collections.Generic;

namespace SampleService.AF.Kps
{
    //
    public class RootType
    {
        private List<string> _empty = new List<string>();
        private StringWithAttrType _StringWithAttr = new StringWithAttrType();

        public List<string> empty { get { return _empty; } } //optional, 
        public string StringElement1 { get; set; } //maxLen: 20, 
        public string StringElement2 { get; set; } //
        public string StringElement3 { get; set; } //pattern: [A-Z]*, 
        public StringWithAttrType StringWithAttr { get { return _StringWithAttr; } } //

    }

    //
    public class StringWithAttrType
    {
    }

}

```

This autogenerated file is included in project (sample.cs), to show that all required helper classes and libraries are included in project
