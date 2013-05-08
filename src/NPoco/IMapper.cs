using System;
using System.Reflection;

namespace NPoco
{
    public interface IMapper
    {
        void GetTableInfo(Type t, TableInfo ti);
        bool MapMemberToColumn(MemberInfo mi, ref string columnName, ref bool resultColumn);
        Func<object, object> GetFromDbConverter(MemberInfo mi, Type SourceType);
        Func<object, object> GetFromDbConverter(Type DestType, Type SourceType);
        Func<object, object> GetParameterConverter(Type SourceType);
        Func<object, object> GetToDbConverter(Type DestType, Type SourceType);
    }
}