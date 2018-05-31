using Log;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;

namespace ClassGenerator.Generator
{
    class XsdContentReader
    {
        readonly string S;
        readonly string S1;
        readonly string S2;
        readonly string S3;

        public XsdContentReader()
        {
            S1 = new String(' ', 4);
            S = new String(' ', 8);
            S2 = new String(' ', 12);
            S3 = new String(' ', 16);
            log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        }

        ILog log;
        bool wasParent, wasVar, wasList, wasItem, wasLong;
        StringBuilder pr, pu, fx, tx;
        Dictionary<string, XmlSchemaSimpleType> simpleTypes;
        Dictionary<string, XmlSchemaComplexType> complexTypes;
        Dictionary<string, List<XmlSchemaComplexType>> listComplexTypes;
        Dictionary<string, string> sameTypes;

        public List<string> GenerateClasses(XsdContentReaderOptions opt)
        {
            XmlSchemaSet ss = new XmlSchemaSet();
            Dictionary<string, string> nsMap = null;

            var fileSet = new Dictionary<string, string>();
            foreach (var f in opt.Files)
            {
                log.Debug("Reading file: " + f.FileName);
                var s = ss.Add(null, f.FileName);
                if (!fileSet.ContainsKey(s.TargetNamespace))
                    fileSet.Add(s.TargetNamespace, Path.GetFileNameWithoutExtension(f.FileName) + ".cs");
                if (f.ShortNamespace != null && s.TargetNamespace != null)
                {
                    if (nsMap == null)
                        nsMap = new Dictionary<string, string>();
                    nsMap.Add(s.TargetNamespace, f.ShortNamespace);
                }
            }
            log.Debug("Compile schema set");
            ss.Compile();

            log.Debug("Empty directory for generated classes");
            const string gDir = "code";
            if (Directory.Exists(gDir))
                Directory.Delete(gDir, true);
            Directory.CreateDirectory(gDir);

            simpleTypes = new Dictionary<string, XmlSchemaSimpleType>();
            complexTypes = new Dictionary<string, XmlSchemaComplexType>();
            listComplexTypes = new Dictionary<string, List<XmlSchemaComplexType>>();
            sameTypes = new Dictionary<string, string>();

            foreach (XmlSchema schema in ss.Schemas())
            {
                var list = new List<XmlSchemaComplexType>();
                if (!listComplexTypes.ContainsKey(schema.TargetNamespace))
                    listComplexTypes.Add(schema.TargetNamespace, list);
                else
                    list = listComplexTypes[schema.TargetNamespace];

                foreach (XmlSchemaType type in schema.SchemaTypes.Values)
                {
                    if (type is XmlSchemaComplexType)
                    {
                        XmlSchemaComplexType ct = type as XmlSchemaComplexType;
                        complexTypes.Add(ct.QualifiedName.ToString(), ct);
                        list.Add(ct);
                    }

                    if (type is XmlSchemaSimpleType)
                    {
                        XmlSchemaSimpleType st = type as XmlSchemaSimpleType;
                        simpleTypes.Add(st.QualifiedName.ToString(), st);
                    }
                }
            }

            int i = 1;
            var namesDict = new Dictionary<string, List<Tuple<string, string>>>();
            foreach (var ns in listComplexTypes.Keys)
            {
                if (listComplexTypes[ns].Count == 0)
                    continue;

                string prefix = "T" + i;
                if (nsMap != null)
                {
                    if (!nsMap.ContainsKey(ns))
                        throw new Exception("Prefix for namespace not specified: " + ns);
                    prefix = nsMap[ns];
                }

                foreach (var ct in listComplexTypes[ns])
                {
                    if (!namesDict.ContainsKey(ct.Name))
                        namesDict.Add(ct.Name, new List<Tuple<string, string>>());
                    namesDict[ct.Name].Add(Tuple.Create(prefix, ct.QualifiedName.ToString()));
                }
            }
            foreach (var k in namesDict)
            {
                if (k.Value.Count > 1)
                {
                    foreach (var v in k.Value)
                        sameTypes.Add(v.Item2, v.Item1 + k.Key);
                }
            }

            var res = new List<string>();
            foreach (var ns in listComplexTypes.Keys)
            {
                if (listComplexTypes[ns].Count == 0)
                    continue;

                string prefix = "ns.nms";
                string prefix2 = "";
                if (nsMap != null)
                {
                    if (!nsMap.ContainsKey(ns))
                        throw new Exception("Prefix for namespace not specified: " + ns);
                    prefix = "ns." + nsMap[ns];
                    prefix2 = nsMap[ns];
                }

                string namesp = opt.CSharpNamespace ?? "Yournamespace";
                var sb = new StringBuilder();
                sb.Append("\n\n *** " + ns + " *** \n\n");
                sb.Append(@"
using " + namesp + @".Xml;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace " + namesp + @".AF.Kps
{");
                foreach (var ct in listComplexTypes[ns])
                {
                    sb.Append("\n" + S1 + "//" + getAnnotation(ct.Annotation) + "\n");
                    sb.Append(S1 + "class " + className(ct) + " : ");
                    var ignoredNames = new Dictionary<string, bool>();
                    wasParent = false;
                    if (ct.BaseXmlSchemaType.QualifiedName.ToString() != "http://www.w3.org/2001/XMLSchema:anyType")
                    {
                        sb.Append(className(ct.BaseXmlSchemaType) + ", ");
                        getIgnoredNames(ct.QualifiedName.ToString(), ignoredNames);
                        wasParent = true;
                    }
                    sb.Append("IXml\n" + S1 + "{");

                    var particle = ct.ContentTypeParticle;
                    if (particle.ToString().EndsWith("EmptyParticle") || particle is XmlSchemaGroupBase)
                    //xs:all, xs:choice, xs:sequence
                    {
                        sb.Append("\n");

                        pr = new StringBuilder();
                        pu = new StringBuilder();
                        fx = new StringBuilder();
                        tx = new StringBuilder();

                        fx.Append(S + "public " + (wasParent ? "new " : "") + "void Init(XElement r)\n" + S + "{\n");
                        if (wasParent)
                            fx.Append(S2 + "base.Init(r);\n\n");
                        tx.Append(S + "public " + (wasParent ? "new " : "") + "XElement ToXElement(XName name, Namespaces ns)\n" + S + "{\n");
                        if (wasParent)
                            tx.Append(S2 + "var r = base.ToXElement(name, ns);\n\n");
                        else
                            tx.Append(S2 + "var r = new XElement(name);\n\n");

                        wasVar = false;
                        wasList = false;
                        wasItem = false;
                        wasLong = false;

                        if (particle is XmlSchemaGroupBase)
                        {
                            XmlSchemaGroupBase baseParticle = particle as XmlSchemaGroupBase;

                            string gName = groupName(baseParticle.ToString());
                            int pos1 = 0;
                            int pos2 = 0;
                            if (gName != "sequence")
                            {
                                pos1 = pu.Length;
                                if (pu.Length > 0)
                                    pu.Append("\n");
                                pu.Append(S + "//" + gName + "\n");
                                pos2 = pu.Length;
                            }
                            bool isChoice = (groupName(baseParticle.ToString()) == "choice");
                            ParseGroup(ignoredNames, prefix, baseParticle, isChoice);
                            if (gName != "sequence")
                            {
                                if (pu.Length == pos2)
                                    pu.Length = pos1;
                                else
                                    pu.Append(S + "//end " + gName + "\n\n");
                            }
                        }

                        fx.Append(S + "}\n");
                        tx.Append("\n" + S2 + "return r;\n" + S + "}\n");

                        sb.Append(pr);
                        if (pr.Length > 0)
                            sb.Append("\n");

                        if (pu.Length > 2 && pu[pu.Length - 1] == '\n' && pu[pu.Length - 2] == '\n')
                            pu.Length--;
                        sb.Append(pu);
                        if (pu.Length > 0)
                            sb.Append("\n");

                        sb.Append(fx);
                        sb.Append("\n");
                        sb.Append(tx);
                    }

                    sb.Append(S1 + "}\n");
                }
                sb.Append("\n}\n");
                res.Add(sb.ToString());

                string path = Path.Combine(gDir, fileSet[ns]);
                log.Debug("write content to " + path + " file");
                using (StreamWriter sw = new StreamWriter(path, false, Encoding.UTF8))
                    sw.Write(sb.ToString().Substring(sb.ToString().IndexOf("using ")));
            }
            return res;
        }

        private void ParseGroup(Dictionary<string, bool> ignoredNames, string prefix, XmlSchemaGroupBase baseParticle, bool isChoice)
        {
            foreach (XmlSchemaParticle subParticle in baseParticle.Items)
            {
                if (subParticle is XmlSchemaGroupBase)
                {
                    int pos1 = pu.Length;
                    if (pu.Length > 0)
                        pu.Append("\n");
                    string gName = groupName(subParticle.ToString());
                    pu.Append(S + "//" + gName + "\n");
                    int pos2 = pu.Length;
                    XmlSchemaGroupBase baseParticle2 = subParticle as XmlSchemaGroupBase;
                    isChoice = isChoice || (groupName(baseParticle2.ToString()) == "choice");
                    ParseGroup(ignoredNames, prefix, baseParticle2, isChoice);
                    if (pu.Length == pos2)
                        pu.Length = pos1;
                    else
                        pu.Append(S + "//end " + gName + "\n\n");
                }
                else if (subParticle is XmlSchemaElement)
                {
                    XmlSchemaElement elem = subParticle as XmlSchemaElement;
                    if (ignoredNames.ContainsKey(elem.QualifiedName.Name))
                        continue;
                    if (wasLong)
                        fx.Append("\n");
                    if (complexTypes.ContainsKey(elem.SchemaTypeName.ToString()) || elem.ElementSchemaType is XmlSchemaComplexType)
                    {
                        var type = (XmlSchemaComplexType)elem.ElementSchemaType ?? complexTypes[elem.SchemaTypeName.ToString()];
                        var typeName = className(type) ?? fixBaseType(type.Datatype.ValueType.Name);
                        if (elem.MaxOccurs > 1)
                        {
                            pr.Append(S + "private List<" + typeName + "> " + uname(elem.Name) + " = new List<" + typeName + ">();\n");
                            pu.Append(S + "public List<" + typeName + "> " + elem.Name + " { get { return " + uname(elem.Name) + "; } } //");

                            if (wasItem && !wasLong)
                                fx.Append("\n");
                            fx.Append(S2 + elem.Name + ".Clear();\n");
                            fx.Append(S2 + (wasList ? "" : "var ") + "list = XmlParser.Elements(r, \"" + elem.Name + "\");\n");
                            wasList = true;
                            fx.Append(S2 + "foreach (var i in list)\n" + S2 + "{\n");
                            fx.Append(S3 + "var o = new " + typeName + "();\n");
                            fx.Append(S3 + "o.Init(i);\n");
                            fx.Append(S3 + elem.Name + ".Add(o);\n" + S2 + "}\n");
                            wasLong = true;

                            tx.Append(S2 + "foreach (var i in " + elem.Name + ")\n");
                            tx.Append(S3 + "r.Add(i.ToXElement(" + prefix + " + \"" + elem.Name + "\", ns));\n");
                        }
                        else
                        {
                            if (elem.MinOccurs == 0 || isChoice)
                            {
                                pu.Append(S + "public " + typeName + " " + elem.Name + " { get; set; } //");

                                if (wasItem && !wasLong)
                                    fx.Append("\n");
                                fx.Append(S2 + (wasVar ? "" : "var ") + "e = XmlParser.Element(r, \"" + elem.Name + "\", false);\n");
                                wasVar = true;
                                fx.Append(S2 + "if (e != null)\n" + S2 + "{\n");
                                fx.Append(S3 + elem.Name + " = new " + typeName + "();\n");
                                fx.Append(S3 + elem.Name + ".Init(e);\n" + S2 + "}\n");
                                wasLong = true;

                                tx.Append(S2 + "if (" + elem.Name + " != null)\n");
                                tx.Append(S3 + "r.Add(" + elem.Name + ".ToXElement(" + prefix + " + \"" + elem.Name + "\", ns));\n");
                            }
                            else if (elem.MinOccurs == 1)
                            {
                                pr.Append(S + "private " + typeName + " " + uname(elem.Name) + " = new " + typeName + "();\n");
                                pu.Append(S + "public " + typeName + " " + elem.Name + " { get { return " + uname(elem.Name) + "; } } //");
                                fx.Append(S2 + elem.Name + ".Init(XmlParser.Element(r, \"" + elem.Name + "\"));\n");
                                tx.Append(S2 + "r.Add(" + elem.Name + ".ToXElement(" + prefix + " + \"" + elem.Name + "\", ns));\n");
                                wasLong = false;
                            }
                        }
                        if (elem.MinOccurs == 0)
                            pu.Append("optional, ");
                        if (elem.MaxOccurs > 1 && elem.MaxOccurs < 1000)
                            pu.Append("maxOccurs: " + elem.MaxOccurs + ", ");
                    }
                    else if (simpleTypes.ContainsKey(elem.SchemaTypeName.ToString()) || elem.ElementSchemaType is XmlSchemaSimpleType)
                    {
                        var type = (XmlSchemaSimpleType)elem.ElementSchemaType ?? simpleTypes[elem.SchemaTypeName.ToString()];
                        if (elem.Name == "RecvDateTime")
                            pr.Append("");
                        var info = getSimpleTypeInfo(type);
                        if (elem.MaxOccurs > 1)
                        {
                            pr.Append(S + "private List<" + info.BaseType + "> " + uname(elem.Name) + " = new List<" + info.BaseType + ">();\n");
                            pu.Append(S + "public List<" + info.BaseType + "> " + elem.Name + " { get { return " + uname(elem.Name) + "; } } //");

                            if (info.BaseType == "string")
                            {
                                if (wasItem && !wasLong)
                                    fx.Append("\n");
                                fx.Append(S2 + elem.Name + ".Clear();\n");
                                fx.Append(S2 + (wasList ? "" : "var ") + "list = XmlParser.Elements(r, \"" + elem.Name + "\");\n");
                                wasList = true;
                                fx.Append(S2 + "foreach (var i in list)\n");
                                fx.Append(S3 + elem.Name + ".Add(i.Value);\n");
                                wasLong = true;

                                tx.Append(S2 + "foreach (var i in " + elem.Name + ")\n");
                                tx.Append(S3 + "r.Add(new XElement(" + prefix + " + \"" + elem.Name + "\", i));\n");
                            }
                        }
                        else
                        {
                            string baseType = info.BaseType;
                            if (elem.MinOccurs == 0 || isChoice)
                            {
                                if (info.BaseType == "string")
                                {
                                    fx.Append(S2 + elem.Name + " = XmlParser.ElementValue(r, \"" + elem.Name + "\", false);\n");
                                    tx.Append(S2 + "if (!string.IsNullOrEmpty(" + elem.Name + "))\n");
                                    tx.Append(S3 + "r.Add(new XElement(" + prefix + " + \"" + elem.Name + "\", " + elem.Name + "));\n");
                                }
                                else if (info.BaseType == "DateTime")
                                {
                                    baseType += "?";
                                    fx.Append(S2 + elem.Name + " = XmlParser.ElementValueDateNull(r, \"" + elem.Name + "\");\n");
                                    tx.Append(S2 + "if (" + elem.Name + " != null)\n");
                                    tx.Append(S3 + "r.Add(new XElement(" + prefix + " + \"" + elem.Name + "\", XmlParser.Date" + (info.TypeCode == "DateTime" ? "Time" : "") + "2Str(" + elem.Name + ".Value)));\n");
                                }
                                else if (info.BaseType == "decimal")
                                {
                                    baseType += "?";
                                    fx.Append(S2 + elem.Name + " = XmlParser.ElementValueDecimalNull(r, \"" + elem.Name + "\");\n");
                                    tx.Append(S2 + "if (" + elem.Name + " != null)\n");
                                    tx.Append(S3 + "r.Add(new XElement(" + prefix + " + \"" + elem.Name + "\", XmlParser.Decimal2Str(" + elem.Name + ".Value)));\n");
                                }
                                else if (info.BaseType == "bool")
                                {
                                    baseType = "string";
                                    fx.Append(S2 + elem.Name + " = XmlParser.ElementValueBool(r, \"" + elem.Name + "\", false);\n");
                                    tx.Append(S2 + "if (!string.IsNullOrEmpty(" + elem.Name + "))\n");
                                    tx.Append(S3 + "r.Add(new XElement(" + prefix + " + \"" + elem.Name + "\", " + elem.Name + "));\n");
                                }
                            }
                            else if (elem.MinOccurs == 1)
                            {
                                if (info.BaseType == "string")
                                {
                                    fx.Append(S2 + elem.Name + " = XmlParser.ElementValue(r, \"" + elem.Name + "\");\n");
                                    tx.Append(S2 + "r.Add(new XElement(" + prefix + " + \"" + elem.Name + "\", " + elem.Name + "));\n");
                                }
                                else if (info.BaseType == "DateTime")
                                {
                                    fx.Append(S2 + elem.Name + " = (DateTime)XmlParser.Element(r, \"" + elem.Name + "\");\n");
                                    tx.Append(S2 + "r.Add(new XElement(" + prefix + " + \"" + elem.Name + "\", XmlParser.Date" + (info.TypeCode == "DateTime" ? "Time" : "") + "2Str(" + elem.Name + ")));\n");
                                }
                                else if (info.BaseType == "decimal")
                                {
                                    fx.Append(S2 + elem.Name + " = (decimal)XmlParser.Element(r, \"" + elem.Name + "\");\n");
                                    tx.Append(S2 + "r.Add(new XElement(" + prefix + " + \"" + elem.Name + "\", XmlParser.Decimal2Str(" + elem.Name + ")));\n");
                                }
                                else if (info.BaseType == "bool")
                                {
                                    baseType = "string";
                                    fx.Append(S2 + elem.Name + " = XmlParser.ElementValueBool(r, \"" + elem.Name + "\");\n");
                                    tx.Append(S2 + "r.Add(new XElement(" + prefix + " + \"" + elem.Name + "\", " + elem.Name + "));\n");
                                }
                            }
                            pu.Append(S + "public " + baseType + " " + elem.Name + " { get; set; } //");
                            wasLong = false;
                        }
                        if (elem.MinOccurs == 0)
                            pu.Append("optional, ");
                        if (!string.IsNullOrEmpty(info.Pattern))
                            pu.Append("pattern: " + info.Pattern + ", ");
                        if (info.MinLen != -1)
                            pu.Append("minLen: " + info.MinLen + ", ");
                        if (info.MaxLen != -1)
                            pu.Append("maxLen: " + info.MaxLen + ", ");
                    }
                    else
                    {
                        throw new Exception("Type not found: " + elem.SchemaTypeName.ToString());
                    }
                    wasItem = true;
                    pu.Append(getAnnotation(elem.Annotation) + "\n");
                }
            }

        }

        private string className(XmlSchemaType type)
        {
            if (sameTypes.ContainsKey(type.QualifiedName.ToString()))
                return sameTypes[type.QualifiedName.ToString()];
            return type.Name;
        }

        private string groupName(string s)
        {
            if (s.StartsWith("System.Xml.Schema.XmlSchema"))
                s = s.Substring("System.Xml.Schema.XmlSchema".Length).ToLower();
            return s;
        }

        private void getIgnoredNames(string name, Dictionary<string, bool> ignoredNames)
        {
            var ct = complexTypes[name];
            var childName = ct.BaseXmlSchemaType.QualifiedName.ToString();
            if (childName != "http://www.w3.org/2001/XMLSchema:anyType")
            {
                //getIgnoredNames(childName, complexTypes, ignoredNames);
                var ctChild = complexTypes[childName];

                var particle = ctChild.ContentTypeParticle;
                XmlSchemaGroupBase baseParticle = particle as XmlSchemaGroupBase;

                foreach (XmlSchemaParticle subParticle in baseParticle.Items)
                    AddIgnoredNames(ignoredNames, subParticle);
            }
        }

        private void AddIgnoredNames(Dictionary<string, bool> ignoredNames, XmlSchemaParticle subParticle)
        {
            if (subParticle is XmlSchemaGroupBase)
            {
                XmlSchemaGroupBase group = subParticle as XmlSchemaGroupBase;

                foreach (XmlSchemaParticle particle in group.Items)
                {
                    AddIgnoredNames(ignoredNames, particle);
                }
            }
            else if (subParticle is XmlSchemaElement)
            {
                XmlSchemaElement elem = subParticle as XmlSchemaElement;
                ignoredNames[elem.QualifiedName.Name] = true;
            }
        }

        private SimpleTypeInfo getSimpleTypeInfo(XmlSchemaSimpleType type)
        {
            string baseType = null;
            string typeCode = null;
            string pattern = null;
            int minLen = -1;
            int maxLen = -1;

            while (type != null)
            {
                string baseType2 = type.Datatype.ValueType.Name;
                string typeCode2 = type.TypeCode.ToString();
                if (typeCode2 == "GYear" || typeCode2 == "Time")
                    baseType2 = "String";
                string pattern2 = getPattern(type);
                int minLen2 = getMinLength(type);
                int maxLen2 = getMaxLength(type);

                if (baseType2 != null && baseType != baseType2)
                    baseType = baseType2;
                if (typeCode2 != null && typeCode != typeCode2)
                    typeCode = typeCode2;
                if (pattern2 != null && pattern == null)
                    pattern = pattern2;
                if (minLen2 != -1 && (minLen2 > minLen || minLen == -1))
                    minLen = minLen2;
                if (maxLen2 != -1 && (maxLen2 < maxLen || maxLen == -1))
                    maxLen = maxLen2;

                string baseName = type.BaseXmlSchemaType.QualifiedName.ToString();
                if (simpleTypes.ContainsKey(baseName))
                    type = simpleTypes[baseName];
                else
                    type = null;
            }

            return new SimpleTypeInfo { BaseType = fixBaseType(baseType), TypeCode = typeCode, Pattern = pattern, MinLen = minLen, MaxLen = maxLen };
        }

        private string fixBaseType(string baseType)
        {
            if (baseType == "String")
                return "string";
            if (baseType == "Decimal")
                return "decimal";
            if (baseType == "Boolean")
                return "bool";
            return baseType;
        }

        class SimpleTypeInfo
        {
            public string BaseType { get; set; }
            public string TypeCode { get; set; }
            public string Pattern { get; set; }
            public int MinLen { get; set; }
            public int MaxLen { get; set; }
        }

        private string getPattern(XmlSchemaSimpleType simpleType)
        {
            var restriction = simpleType.Content as XmlSchemaSimpleTypeRestriction;
            if (restriction == null)
                return null;

            string result = "";
            foreach (XmlSchemaObject facet in restriction.Facets)
            {
                if (facet is XmlSchemaPatternFacet)
                    result = ((XmlSchemaFacet)facet).Value;
            }
            return result;
        }

        private int getMaxLength(XmlSchemaSimpleType simpleType)
        {
            var restriction = simpleType.Content as XmlSchemaSimpleTypeRestriction;
            if (restriction == null)
                return -1;

            int result = -1;
            foreach (XmlSchemaObject facet in restriction.Facets)
            {
                if (facet is XmlSchemaMaxLengthFacet)
                    result = int.Parse(((XmlSchemaFacet)facet).Value);
            }
            return result;
        }

        private int getMinLength(XmlSchemaSimpleType simpleType)
        {
            var restriction = simpleType.Content as XmlSchemaSimpleTypeRestriction;
            if (restriction == null)
                return -1;

            int result = -1;
            foreach (XmlSchemaObject facet in restriction.Facets)
            {
                if (facet is XmlSchemaMinLengthFacet)
                    result = int.Parse(((XmlSchemaFacet)facet).Value);
            }
            return result;
        }

        private string uname(string name)
        {
            return "_" + name;// name[0].ToString().ToLower() + name.Substring(1);
        }

        private string getAnnotation(XmlSchemaAnnotation an)
        {
            string str = "";
            if (an != null && an.Items != null)
            {
                foreach (XmlSchemaObject annotation in an.Items)
                {
                    if (annotation is XmlSchemaDocumentation)
                    {
                        foreach (XmlNode doc in ((XmlSchemaDocumentation)annotation).Markup)
                        {
                            if (str.Length > 0)
                                str += " ";
                            str += doc.InnerText;
                        }
                    }
                }
            }
            str = str.Replace('\n', ' ');
            while (str.IndexOf("  ") != -1)
                str = str.Replace("  ", " ");
            return str;
        }
    }
}
