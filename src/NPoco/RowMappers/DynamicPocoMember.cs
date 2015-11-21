using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace NPoco.RowMappers
{
    public class DynamicPocoMember : PocoMember
    {
        private readonly MapperCollection _mapperCollection;

        public DynamicPocoMember(MapperCollection mapperCollection)
        {
            _mapperCollection = mapperCollection;
        }

        public override object Create(DbDataReader dataReader)
        {
            return _mapperCollection.GetFactory(MemberInfoData.MemberType)(dataReader);
        }

        public override void SetValue(object target, object value)
        {
            ((IDictionary<string, object>) target)[Name] = value;
        }

        public override object GetValue(object target)
        {
            object val;
            ((IDictionary<string, object>)target).TryGetValue(Name, out val);
            return val;
        }
    }
}