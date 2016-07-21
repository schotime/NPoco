using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace NPoco
{
    public interface IIdentityGenerator
    {
        long Generate<T>();
        long Generate(Type type);        
    }

    public class LinearBlockIndentityGenerator : IIdentityGenerator
    {
        public string TableName { get; set; }
        public string KeyColumn { get; set; }
        public string ValueColumn { get; set; }

        public long BlockSize { get; set; }

        private static Dictionary<Type, Data> _data = new Dictionary<Type, Data>();
        private static object _lock = new object();

        private readonly Func<IDatabase> _databaseFactory;

        public LinearBlockIndentityGenerator(Func<IDatabase> databaseFactory)
        {
            _databaseFactory = databaseFactory;

            TableName = "NPocoIds";
            KeyColumn = "id";
            ValueColumn = "nextval";
            BlockSize = 50;
        }
        
        public struct Data
        {
            public long allocNext;
            public long allocHi;
        }              

        public long Generate<T>()
        {
            return Generate(typeof(T));
        }

        public long Generate(Type type)
        {
            lock (_lock)
            {
                var data = _data.ContainsKey(type) ? _data[type] : new Data();

                if (data.allocNext >= data.allocHi)
                {
                    long allocated = AllocateBlock(type);
                    data.allocNext = allocated;
                    data.allocHi = allocated + BlockSize;
                }

                data.allocNext = data.allocNext + 1;
                _data[type] = data;                
                return data.allocNext;
            }            
        }

        private long AllocateBlock(Type type)
        {
            long? result = 0;
            int rows;

            using (var db = _databaseFactory())
            {
                var pocoData = db.PocoDataFactory.ForType(type);
                var querySql = string.Format("select {0} from {1} where {2} = @0", db.DatabaseType.EscapeSqlIdentifier(ValueColumn), db.DatabaseType.EscapeTableName(TableName), db.DatabaseType.EscapeSqlIdentifier(KeyColumn));
                var updateSql = string.Format("update {0} set {1} = @0 where {2} = @1 and {1} = @2", db.DatabaseType.EscapeTableName(TableName), db.DatabaseType.EscapeSqlIdentifier(ValueColumn), db.DatabaseType.EscapeSqlIdentifier(KeyColumn));
                var insertSql = string.Format("insert into {0} ({1}, {2}) select @0, @1 /*poco_dual*/ where not exists (select {1} from {0} where {1} = @0)", db.DatabaseType.EscapeTableName(TableName), db.DatabaseType.EscapeSqlIdentifier(KeyColumn), db.DatabaseType.EscapeSqlIdentifier(ValueColumn));

                do
                {
                    // The loop ensures atomicity of the select + update even for no transaction or read committed isolation level
                    try
                    {
                        result = db.ExecuteScalar<long?>(querySql, pocoData.TableInfo.TableName);

                        if (result == null)
                        {
                            db.Execute(insertSql, pocoData.TableInfo.TableName, 0);
                            result = 0;
                        }

                        rows = db.Execute(updateSql, result + BlockSize, pocoData.TableInfo.TableName, result);
                    }
                    catch (Exception sqle)
                    {
                        throw new Exception("Could not retrieve value", sqle);
                    }

                } while (rows == 0);
            }

            return result.Value;
        }
    }
}
