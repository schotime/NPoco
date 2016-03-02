using System;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;

namespace NPoco
{
    public class SqlServerDefaultMapper : DefaultMapper
    {
        public override Func<object, object> GetToDbConverter(Type destType, MemberInfo sourceMemberInfo)
        {
            if (sourceMemberInfo.GetMemberInfoType() == typeof(byte[]))
            {
                return x => x ?? new SqlParameter("__bytes", SqlDbType.VarBinary, -1) { Value = DBNull.Value };
            }
            return base.GetToDbConverter(destType, sourceMemberInfo);
        }
    }
}