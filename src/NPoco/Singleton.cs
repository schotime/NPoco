using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NPoco
{
    static class Singleton<T> where T : new()
    {
        public static T Instance = new T();
    }

    class DynamicDatabaseType
    {
        public static Cache<string, DatabaseType> cache = Cache<string, DatabaseType>.CreateStaticCache();

        public static DatabaseType MakeSqlServerType(string type)
        {
            try
            {
                return cache.Get(type, () =>
                {
                    var newType = Type.GetType($"NPoco.DatabaseTypes.{type}, NPoco.SqlServer");
                    var gen = typeof(Singleton<>).MakeGenericType(newType);
                    return (DatabaseType)gen.GetField("Instance").GetValue(null);
                });                
            }
            catch (Exception ex)
            {
                throw new Exception($"No database type found for the type string specified: '{type}'. Make sure the relevant assembly NPoco.SqlServer is referenced.", ex);
            }
        }
    }
}
