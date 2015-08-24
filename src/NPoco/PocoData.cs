using System;
using System.Collections.Generic;
using System.Linq;

namespace NPoco
{
    public class PocoData
    {
        public static string Separator = "__";

        public Type Type { get; private set; }
        public MapperCollection Mapper { get; private set; }

        public KeyValuePair<string, PocoColumn>[] QueryColumns { get; protected internal set; }
        public TableInfo TableInfo { get; protected internal set; }
        public Dictionary<string, PocoColumn> Columns { get; protected internal set; }
        public List<PocoMember> Members { get; protected internal set; }
        public List<PocoColumn> AllColumns { get; protected internal set; }

        public PocoData()
        {
        }

        public PocoData(Type type, MapperCollection mapper) : this()
        {
            Type = type;
            Mapper = mapper;
        }
        
        public object[] GetPrimaryKeyValues(object obj)
        {
            return PrimaryKeyValues(obj);
        }

        private Func<object, object[]> _primaryKeyValues;
        private Func<object, object[]> PrimaryKeyValues
        {
            get
            {
                if (_primaryKeyValues == null)
                {
                    var multiplePrimaryKeysNames = TableInfo.PrimaryKey.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
                    var members = multiplePrimaryKeysNames
                        .Select(x => Members.FirstOrDefault(y => y.PocoColumn != null
                                && y.ReferenceMappingType == ReferenceMappingType.None
                                && string.Equals(x, y.PocoColumn.ColumnName, StringComparison.OrdinalIgnoreCase)))
                        .Where(x => x != null);
                    _primaryKeyValues = obj => members.Select(x => x.PocoColumn.GetValue(obj)).ToArray();
                }
                return _primaryKeyValues;
            }
        }

        public object CreateObject()
        {
            if (CreateDelegate == null)
                CreateDelegate = new FastCreate(Type);
            return CreateDelegate.Create();
        }

        private FastCreate CreateDelegate { get; set; }

    }
}
