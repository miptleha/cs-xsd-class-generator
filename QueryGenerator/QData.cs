using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryGenerator
{
    public class QData
    {
        Dictionary<string, QTable> _tabs = new Dictionary<string, QTable>();
        QExtHierarchy _root = new QExtHierarchy();

        public Dictionary<string, QTable> Tables { get { return _tabs; } }
        public QExtHierarchy Root { get { return _root; } }

        /// <summary>
        /// Add description of class. Should by called recursively for child classes.
        /// </summary>
        /// <param name="tab">description of table in db (without fields)</param>
        /// <param name="h">description of hierarchy of class</param>
        /// <param name="fields">description of fields with simple types</param>
        public void AddInfo(QTable tab, QHierarchy h, params QField[] fields)
        {
            if (string.IsNullOrWhiteSpace(tab.Name))
                throw new Exception("Table name can not be empty");

            foreach (var f in fields)
            {
                if (string.IsNullOrWhiteSpace(f.Name))
                    throw new Exception("Name of field can not be empty for table: " + tab.Name);
                if (string.IsNullOrEmpty(f.Prefix))
                    f.Prefix = tab.Prefix;
                if (h != null && string.IsNullOrEmpty(h.Name))
                    throw new Exception("Name not set in hierarchy for table: " + tab.Name);
                f.Hierarchy = h;
            }

            if (!_tabs.ContainsKey(tab.Name))
            {
                _tabs[tab.Name] = tab;
                tab.Fields.AddRange(fields);
            }
            else
            {
                var t = _tabs[tab.Name];
                if (string.IsNullOrEmpty(t.Comment))
                    t.Comment = tab.Comment;
                t.Fields.AddRange(fields);
            }

            if (h == null)
            {
                if (_root.Table == null)
                    _root.Table = tab;
                else if (_root.Table.Name != tab.Name)
                    throw new Exception("For root node table name was " + _root.Table.Name + ", new table name: " + tab.Name);

                _root.Fields.AddRange(fields);
            }
            else
            {
                var ehParent = h.Parent == null ? _root : FindParent(h.Parent, _root);
                if (ehParent == null)
                    throw new Exception("Parent not found for table: " + tab.Name + ", hierarchy: " + h.Path);

                bool found = false;
                foreach (var ehChild in ehParent.Children)
                {
                    if (ehChild.Hierarchy.Name == h.Name)
                    {
                        if (ehChild.Hierarchy.Type != h.Type)
                            throw new Exception("Different types for one name: " + h.Name + ", parent: " + ehParent.Hierarchy.Path);
                        ehChild.Fields.AddRange(fields);
                        found = true;
                    }
                }

                if (!found)
                {
                    var eh = new QExtHierarchy();
                    eh.Hierarchy = h;
                    eh.Table = tab;
                    eh.Fields.AddRange(fields);
                    ehParent.Children.Add(eh);
                }
            }

            var checkUnique = new Dictionary<string, QHierarchy>();
            foreach (var f in tab.Fields)
            {
                string fname = f.FullName;
                if (checkUnique.ContainsKey(fname.ToLower()))
                    throw new Exception("Table " + tab.Name + " has multiple columns with name: '" + fname + "' in " + (f.Hierarchy != null ? f.Hierarchy.Path : "parent") + " and " + checkUnique[fname.ToLower()].Path);
                checkUnique.Add(fname.ToLower(), f.Hierarchy ?? new QHierarchy("Root", QHType.Member, null));
            }
        }

        private QExtHierarchy FindParent(QHierarchy h, QExtHierarchy p)
        {
            foreach (var eh in p.Children)
            {
                if (eh.Hierarchy.Path == h.Path)
                    return eh;
                var f = FindParent(h, eh);
                if (f != null)
                    return f;
            }
            return null;
        }
    }

    /// <summary>
    /// One field in cs class (of simple type) or one field in database
    /// </summary>
    public class QField
    {
        /// <summary>
        /// Name in db is Prefix_Name
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// Name is cs
        /// </summary>
        public string NameCs { get; set; }

        /// <summary>
        /// If class is simple type (string) and in db stored obj (not obj.prop)
        /// </summary>
        public bool NoNameCs { get; set; }

        /// <summary>
        /// Name of field in db and property in cs (if NameCs not specified)
        /// </summary>
        public string Name { get; set; }

        public string FullName
        {
            get
            {
                return (Prefix != null ? Prefix + "_" : "") + Name;
            }
        }

        /// <summary>
        /// Comment of field in db
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Set for declare field as not null in db
        /// </summary>
        public bool NotNull { get; set; }

        /// <summary>
        /// Default value for field in db
        /// </summary>
        public string Default { get; set; }

        /// <summary>
        /// Size of simple type (Number(10) or Varchar2(10))
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// For list of strings set max count of list 
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Type of field (string, date, number)
        /// </summary>
        public QType Type { get; set; }

        /// <summary>
        /// No need to specify, taken from 2 argument of AddInfo()
        /// </summary>
        public QHierarchy Hierarchy { get; set; }
    }

    /// <summary>
    /// Description of table in database
    /// </summary>
    public class QTable
    {
        /// <summary>
        /// Name of table in db
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Comment of table in db
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Prefix for all fields
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// Name of field for primary key (field will be generated automaticaly)
        /// </summary>
        public string Pk { get; set; }

        /// <summary>
        /// Comment for field that is primary key
        /// </summary>
        public string PkComment { get; set; }

        /// <summary>
        /// Name of field for secondary key (field will be generated automaticaly)
        /// </summary>
        public string Fk { get; set; }

        /// <summary>
        /// Comment for field that is secondary key
        /// </summary>
        public string FkComment { get; set; }

        List<QField> _fields = new List<QField>();
        public List<QField> Fields { get { return _fields; } }
    }

    /// <summary>
    /// Type of QField
    /// </summary>
    public enum QType
    {
        Number,
        String,
        StringList, //list of string stored in one field, separeted by ',' (specify big length property for QField)
        StringMulti, //list of string each stored in own fields with names: [name]1, name[2], ... (length of fields in Size property, count in Count property of QField)
        Date
    }

    /// <summary>
    /// Description of property hierarchy
    /// Name - CSharp name of property
    /// Type - Is property single or list
    /// Parent - Hierarchy description of parent class
    /// </summary>
    public class QHierarchy
    {
        public QHierarchy(string name, QHType type, QHierarchy parent)
        {
            Name = name;
            Type = type;
            Parent = parent;
        }

        public string Name { get; set; }
        public QHType Type { get; set; }
        public QHierarchy Parent { get; set; }

        public string Path
        {
            get
            {
                return Parent != null ? Parent.Path + "." + Name : Name;
            }
        }
    }

    /// <summary>
    /// Type of hierarchy (not inheritance). It is type of member: single of list
    /// </summary>
    public enum QHType
    {
        Member,
        List
    }

    /// <summary>
    /// Internal type
    /// </summary>
    public class QExtHierarchy
    {
        List<QExtHierarchy> _children = new List<QExtHierarchy>();
        public List<QExtHierarchy> Children { get { return _children; } }

        List<QField> _fields = new List<QField>();
        public List<QField> Fields { get { return _fields; } }

        public QHierarchy Hierarchy { get; set; }
        public QTable Table { get; set; }
    }
}
