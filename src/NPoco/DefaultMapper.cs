using System;
using System.Reflection;

namespace NPoco
{
    public abstract class DefaultMapper : IMapper
    {
        public virtual void GetTableInfo(Type t, TableInfo ti) { }
 
        public virtual bool MapMemberToColumn(MemberInfo pi, ref string columnName, ref bool resultColumn)
        {
            return true;
        }
        public virtual Func<object, object> GetFromDbConverter(MemberInfo mi, Type SourceType)
        {
            var t = mi.GetMemberInfoType();
            return mi != null ? GetFromDbConverter(t, SourceType) : null;
        }
        public virtual Func<object, object> GetParameterConverter(Type SourceType)
        {
            return null;
        }
        public virtual Func<object, object> GetFromDbConverter(Type DestType, Type SourceType)
        {
            return null;
        }
        public virtual Func<object, object> GetToDbConverter(Type DestType, Type SourceType)
        {
            return null;
        }
    }
}