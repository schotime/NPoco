using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;

namespace NPoco
{
    public interface IMapper
    {
        Func<object, object> GetFromDbConverter(MemberInfo memberInfo, Type sourceType, IReadOnlyDictionary<string, object> metadata = null);
        Func<object, object> GetFromDbConverter(Type destType, Type sourceType);
        Func<object, object> GetParameterConverter(DbCommand dbCommand, Type sourceType);
        Func<object, object> GetToDbConverter(Type destType, MemberInfo sourceMemberInfo, IReadOnlyDictionary<string, object> metadata = null);
    }
}