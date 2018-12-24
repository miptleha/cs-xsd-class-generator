using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Misc
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
                throw new Exception(string.Format("Invalid root element: '{0}'. Expect: '{1}'.", r.Name.LocalName, name));
        }

        public static int? ToIntNull(object val)
        {
            if (val == null || val == DBNull.Value)
                return null;
            return ToInt(val);
        }

        public static int ToInt(object val)
        {
            return int.Parse(val.ToString());
        }

        public static decimal? ToDecimalNull(object val)
        {
            if (val == null || val == DBNull.Value)
                return null;
            return ToDecimal(val);
        }

        public static decimal ToDecimal(object val)
        {
            if (val is decimal)
                return (decimal)val;
            else if (val is int)
                return (decimal)(int)val;
            else if (val is double)
                return Convert.ToDecimal((double)val);
            else
                throw new Exception("Попытка приведения типа " + val.GetType() + " к decimal");
        }
    }
}
