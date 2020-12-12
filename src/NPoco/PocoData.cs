using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
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
        private IFastCreate CreateDelegate { get; }

        // This is used on a per query basis, if we have cache PocoData then this will need to change.
        public bool IsQueryGenerated { get; set; }

        public PocoData(Type type, MapperCollection mapper, IFastCreate creator)
        {
            CreateDelegate = creator;
            Type = type;
            Mapper = mapper;
        }
        
        public object[] GetPrimaryKeyValues(object obj)
        {
            return PrimaryKeyValues(obj);
        }

        public IEnumerable<PocoMember> GetAllMembers()
        {
            return GetAllMembers(Members);
        }

        private IEnumerable<PocoMember> GetAllMembers(IEnumerable<PocoMember> pocoMembers)
        {
            foreach (var member in pocoMembers)
            {
                yield return member;
                foreach(var childmember in GetAllMembers(member.PocoMemberChildren))
                {
                    yield return childmember;
                }
            }
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
                                && y.ReferenceType == ReferenceType.None
                                && string.Equals(x, y.PocoColumn.ColumnName, StringComparison.OrdinalIgnoreCase)))
                        .Where(x => x != null);
                    _primaryKeyValues = obj => members.Select(x => x.PocoColumn.GetValue(obj)).ToArray();
                }
                return _primaryKeyValues;
            }
        }

        public object CreateObject(DbDataReader dataReader)
        {
            return CreateDelegate.Create(dataReader);
        }
    }
}
