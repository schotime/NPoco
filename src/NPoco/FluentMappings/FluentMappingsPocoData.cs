using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NPoco.FluentMappings
{
    public class FluentMappingsPocoData : PocoData
    {
        private readonly TypeDefinition _typeConfig;

        public FluentMappingsPocoData(Type type, TypeDefinition typeConfig, IMapper mapper, Cache<string, Type> aliasToTypeCache, PocoDataFactory pocoDataFactory) : 
            base(type, mapper, aliasToTypeCache, pocoDataFactory)
        {
            _typeConfig = typeConfig;
        }

        protected override string GetColumnName(string prefix, string columnName)
        {
            return columnName;
        }

        protected override TableInfo GetTableInfo(Type type)
        {
            var tableInfo = new TableInfo();

            // Get the table name
            var a = _typeConfig.TableName ?? "";
            tableInfo.TableName = a.Length == 0 ? type.Name : a;

            // Get the primary key
            a = _typeConfig.PrimaryKey ?? "";
            tableInfo.PrimaryKey = a.Length == 0 ? "ID" : a;

            a = _typeConfig.SequenceName ?? "";
            tableInfo.SequenceName = a.Length == 0 ? null : a;

            tableInfo.AutoIncrement = _typeConfig.AutoIncrement ?? true;

            // Set autoincrement false if primary key has multiple columns
            tableInfo.AutoIncrement = tableInfo.AutoIncrement ? !tableInfo.PrimaryKey.Contains(',') : tableInfo.AutoIncrement;

            return tableInfo;
        }

        protected override ColumnInfo GetColumnInfo(MemberInfo mi, MemberInfo[] memberInfos)
        {
            var columnInfo = new ColumnInfo();
            var key = string.Join("__", memberInfos.Select(x => x.Name).Concat(new[] { mi.Name }));
            
            bool explicitColumns = _typeConfig.ExplicitColumns ?? false;
            var isColumnDefined = _typeConfig.ColumnConfiguration.ContainsKey(key);

            if (isColumnDefined && _typeConfig.ColumnConfiguration[key].IsComplexMapping)
            {
                if (_typeConfig.ColumnConfiguration[key].IgnoreColumn.HasValue && _typeConfig.ColumnConfiguration[key].IgnoreColumn.Value)
                    columnInfo.IgnoreColumn = true;

                columnInfo.ComplexMapping = true;
                return columnInfo;
            }

            if (explicitColumns && !isColumnDefined)
                columnInfo.IgnoreColumn = true;

            if (isColumnDefined && (_typeConfig.ColumnConfiguration[key].IgnoreColumn.HasValue && _typeConfig.ColumnConfiguration[key].IgnoreColumn.Value))
                columnInfo.IgnoreColumn = true;

            // Work out the DB column name
            if (isColumnDefined)
            {
                var colattr = _typeConfig.ColumnConfiguration[key];
                columnInfo.ColumnName = colattr.DbColumnName;
                columnInfo.ColumnAlias = colattr.DbColumnAlias;
                if (colattr.ResultColumn.HasValue && colattr.ResultColumn.Value)
                {
                    columnInfo.ResultColumn = true;
                }
                else if (colattr.VersionColumn.HasValue && colattr.VersionColumn.Value)
                {
                    columnInfo.VersionColumn = true;
                    columnInfo.VersionColumnType = colattr.VersionColumnType ?? VersionColumnType.Number;
                }
                else if (colattr.ComputedColumn.HasValue && colattr.ComputedColumn.Value)
                {
                    columnInfo.ComputedColumn = true;
                }

                if (colattr.ForceUtc.HasValue && colattr.ForceUtc.Value)
                {
                    columnInfo.ForceToUtc = true;
                }

                columnInfo.ColumnType = colattr.DbColumnType;

                if (memberInfos.Length == 0)
                {
                    var originalPk = TableInfo.PrimaryKey.Split(',');
                    for (int i = 0; i < originalPk.Length; i++)
                    {
                        if (originalPk[i].Equals(mi.Name, StringComparison.OrdinalIgnoreCase))
                            originalPk[i] = (columnInfo.ColumnName ?? mi.Name);
                    }
                    TableInfo.PrimaryKey = String.Join(",", originalPk);
                }
            }
            else
            {
                columnInfo.IgnoreColumn = true;
            }
            
            return columnInfo;
        }
    }
}
