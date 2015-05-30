using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NPoco.FluentMappings
{
    public class FluentMappingsPocoData : PocoData
    {
        private readonly Mappings _mappings;

        public FluentMappingsPocoData(Type type, Mappings mappings, IMapper mapper, PocoDataFactory pocoDataFactory) : 
            base(type, mapper, pocoDataFactory)
        {
            _mappings = mappings;
        }

        protected override string GetColumnName(string prefix, string columnName)
        {
            return columnName;
        }

        protected override TableInfo GetTableInfo(Type type)
        {
            var typeConfig = _mappings.Config[type];
            var tableInfo = new TableInfo();

            // Get the table name
            var a = typeConfig.TableName ?? "";
            tableInfo.TableName = a.Length == 0 ? type.Name : a;

            // Get the primary key
            a = typeConfig.PrimaryKey ?? "";
            tableInfo.PrimaryKey = a.Length == 0 ? "ID" : a;

            a = typeConfig.SequenceName ?? "";
            tableInfo.SequenceName = a.Length == 0 ? null : a;

            tableInfo.AutoIncrement = typeConfig.AutoIncrement ?? true;

            // Set autoincrement false if primary key has multiple columns
            tableInfo.AutoIncrement = tableInfo.AutoIncrement ? !tableInfo.PrimaryKey.Contains(',') : tableInfo.AutoIncrement;
            
            // Set auto alias
            tableInfo.AutoAlias = CreateAlias(type.Name, type);

            return tableInfo;
        }

        protected override ColumnInfo GetColumnInfo(MemberInfo mi, MemberInfo[] memberInfos)
        {
            var typeConfig = _mappings.Config[Type];
            var columnInfo = new ColumnInfo();
            var key = PocoColumn.GenerateKey(memberInfos.Concat(new[] { mi }));

            bool explicitColumns = typeConfig.ExplicitColumns ?? false;
            var isColumnDefined = typeConfig.ColumnConfiguration.ContainsKey(key);

            if (isColumnDefined && typeConfig.ColumnConfiguration[key].IsComplexMapping)
            {
                if (typeConfig.ColumnConfiguration[key].IgnoreColumn.HasValue && typeConfig.ColumnConfiguration[key].IgnoreColumn.Value)
                    columnInfo.IgnoreColumn = true;

                columnInfo.ComplexMapping = true;
                return columnInfo;
            }

            if (isColumnDefined && typeConfig.ColumnConfiguration[key].IsReferenceMember.HasValue && typeConfig.ColumnConfiguration[key].IsReferenceMember.Value)
            {
                if (typeConfig.ColumnConfiguration[key].ReferenceMappingType != null)
                    columnInfo.ReferenceMappingType = typeConfig.ColumnConfiguration[key].ReferenceMappingType.Value;

                if (typeConfig.ColumnConfiguration[key].ReferenceMember != null)
                    columnInfo.ReferenceMemberName = typeConfig.ColumnConfiguration[key].ReferenceMember.Name;
            }

            if (explicitColumns && !isColumnDefined)
                columnInfo.IgnoreColumn = true;

            if (isColumnDefined && (typeConfig.ColumnConfiguration[key].IgnoreColumn.HasValue && typeConfig.ColumnConfiguration[key].IgnoreColumn.Value))
                columnInfo.IgnoreColumn = true;

            // Work out the DB column name
            if (isColumnDefined)
            {
                var colattr = typeConfig.ColumnConfiguration[key];
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
