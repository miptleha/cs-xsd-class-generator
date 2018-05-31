using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ClassGenerator.AF
{
    class Util
    {
        public static string ToStr(object val)
        {
            if (val == null || val == DBNull.Value)
                return null;
            return val.ToString();
        }

        public static DateTime ToDate(object val)
        {
            if (val == null || val == DBNull.Value)
                return DateTime.MinValue;
            return (DateTime)val;
        }

        public static DateTime? ToDateNull(object val)
        {
            if (val == null || val == DBNull.Value)
                return null;
            return (DateTime)val;
        }

        public static void CheckName(XElement r, string name)
        {
            if (r.Name.LocalName != name)
                throw new Exception(string.Format("Неверный корневой элемент в теле сообщения: '{0}'. Ожидается: '{1}'.", r.Name.LocalName, name));
        }
    }
}
