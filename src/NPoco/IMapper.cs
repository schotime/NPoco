using System;
using System.Data;
using System.Reflection;

namespace NPoco
{
    public interface IMapper
    {
        void GetTableInfo(Type t, TableInfo ti);
        bool MapMemberToColumn(MemberInfo mi, ref string columnName, ref bool resultColumn);
        Func<object, object> GetFromDbConverter(MemberInfo mi, Type sourceType);
        Func<object, object> GetFromDbConverter(Type destType, Type sourceType);
        Func<object, object> GetParameterConverter(IDbCommand dbCommand, Type sourceType);
        Func<object, object> GetToDbConverter(Type destType, Type sourceType);
        Func<object, object> GetToDbConverter(Type destType, MemberInfo sourceMemberInfo);
    }
}