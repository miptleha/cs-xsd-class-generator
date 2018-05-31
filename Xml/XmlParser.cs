using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml;
using ClassGenerator.AF;
using System.Globalization;

namespace ClassGenerator.Xml
{
    class XmlParser
    {
        public static string ToXml<T>(T obj, XName name, Namespaces ns, bool xmlDeclaration = true) where T: IXml
        {
            XElement e = obj.ToXElement(name, ns);
            XDocument d = new XDocument();
            d.Add(e);

            string res = null;
            if (xmlDeclaration)
            {
                d.Declaration = new XDeclaration("1.0", "UTF-8", null);
                res = d.Declaration.ToString() + Environment.NewLine + d.ToString();
            }
            else
            {
                res = d.ToString();
            }
            return res;
        }

        public static T FromXml<T>(string xml) where T: IXml, new()
        {
            var e = XElement.Parse(xml);
            return FromXml<T>(e);
        }

        public static T FromXml<T>(XElement e) where T: IXml, new()
        {
            var t = new T();
            t.Init(e);
            return t;
        }

        public static void Validate(string xml, params string[] schemaPath)
        {
            //var d = XDocument.Parse(xml);
            //var sc = new XmlSchemaSet();
            //sc.Add(null, schemaPath);
            //sc.CompilationSettings.EnableUpaCheck = false;
            //d.Validate(sc, null);

            XmlReaderSettings Xsettings = new XmlReaderSettings();
            foreach (var path in schemaPath)
                Xsettings.Schemas.Add(null, path);
            Xsettings.ValidationType = ValidationType.Schema;
            Xsettings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
            Xsettings.Schemas.CompilationSettings.EnableUpaCheck = false;
            Xsettings.ValidationEventHandler += new ValidationEventHandler(ValidationCallBack);

            XmlReader reader = XmlReader.Create(new StringReader(xml), Xsettings);
            while (reader.Read())
            {
            }
        }

        private static void ValidationCallBack(object sender, ValidationEventArgs e)
        {
            throw e.Exception;
        }

        public static XAttribute Attribute(XElement e, string localName, bool required = true)
        {
            var c = e.Attributes().FirstOrDefault(i => i.Name.LocalName == localName);
            if (c == null && required)
                throw new Exception(string.Format("The required attribute '{0}' not found inside the element '{1}'", localName, e.Name.LocalName));
            return c;
        }

        public static XElement Element(XElement e, string localName, bool required = true)
        {
            var c = e.Elements().FirstOrDefault(i => i.Name.LocalName == localName);
            if (c == null && required)
                throw new Exception(string.Format("The required element '{0}' not found inside the element '{1}'", localName, e.Name.LocalName));
            return c;
        }

        public static string ElementValue(XElement e, string localName, bool required = true)
        {
            var c = Element(e, localName, required);
            return c != null ? c.Value : null;
        }

        public static string ElementValueBool(XElement e, string localName, bool required = true)
        {
            var c = Element(e, localName, required);
            if (c == null)
                return null;

            var b = c.Value;
            if (b.ToLower() == "true" || b == "1")
                b = "1";
            else if (b.ToLower() == "false" || b == "0")
                b = "0";
            else
                throw new Exception(string.Format("Invalid value '{0}' for boolean element '{1}'", c.Value, localName));
    
            return b;    
        }

        public static DateTime? ElementValueDateNull(XElement e, string localName)
        {
            var c = Element(e, localName, false);
            return c != null ? (DateTime?)(DateTime)c : null;
        }

        public static decimal? ElementValueDecimalNull(XElement e, string localName)
        {
            var c = Element(e, localName, false);
            return c != null ? (decimal?)(decimal)c : null;
        }

        public static IEnumerable<XElement> Elements(XElement e, string localName)
        {
            var list = e.Elements().Where(i => i.Name.LocalName == localName);
            return list;
        }

        public static string Decimal2Str(decimal d)
        {
            return d.ToString(CultureInfo.InvariantCulture);
        }

        public static string Date2Str(DateTime dt)
        {
            return dt.ToString("yyyy-MM-dd");
            //return dt.ToString("u").Replace(" ", "T").Replace("Z", "");
        }

        public static string DateTime2Str(DateTime dt)
        {
            return dt.ToString("yyyy-MM-ddTHH:mm:sszzz");
            //return dt.ToString("yyyy-MM-ddTHH:mm:ssZ");
        }

        //public static DateTime FromXmlDate(string str)
        //{
        //    return DateTime.ParseExact(str, "yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
        //}
    }
}
