using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NPoco.FluentMappings
{
    public class FluentMappingsPocoDataBuilder : PocoDataBuilder
    {
        private readonly Mappings _mappings;

        public FluentMappingsPocoDataBuilder(Type type, Mappings mappings, MapperCollection mapper, PocoDataFactory pocoDataFactory) : 
            base(type, mapper, pocoDataFactory)
        {
            _mappings = mappings;
        }

        protected override string GetColumnName(string prefix, string columnName)
        {
            return columnName;
        }

        protected override TableInfoPlan GetTableInfo(Type type, ColumnInfo[] columnInfos, List<MemberInfo> memberInfos)
        {
            var typeConfig = _mappings.Config[type];
            // Get the table name
            var a = typeConfig.TableName ?? "";
            var tableName = a.Length == 0 ? type.Name : a;

            // Get the primary key
            a = typeConfig.PrimaryKey ?? "";
            var primaryKey = a.Length == 0 ? "ID" : a;

            if (memberInfos.Count == 0) // if top level
            {
                foreach (var ci in columnInfos)
                {
                    var originalPk = primaryKey.Split(',');
                    for (int i = 0; i < originalPk.Length; i++)
                    {
                        if (originalPk[i].Equals(ci.MemberInfo.Name, StringComparison.OrdinalIgnoreCase))
                            originalPk[i] = (ci.ColumnName ?? ci.MemberInfo.Name);
                    }
                    primaryKey = string.Join(",", originalPk);
                }
            }
            
            a = typeConfig.SequenceName ?? "";
            var sequenceName = a.Length == 0 ? null : a;

            var autoIncrement = typeConfig.AutoIncrement ?? true;

            // Set autoincrement false if primary key has multiple columns
            autoIncrement = autoIncrement ? !primaryKey.Contains(',') : autoIncrement;
            
            // Set auto alias
            var autoAlias = CreateAlias(type.Name, type);
            
            return () => new TableInfo
            {
                TableName = tableName,
                PrimaryKey = primaryKey,
                SequenceName = sequenceName,
                AutoIncrement = autoIncrement,
                AutoAlias = autoAlias
            };
        }

        protected override ColumnInfo GetColumnInfo(MemberInfo mi, MemberInfo[] memberInfos)
        {
            var typeConfig = _mappings.Config[Type];
            var columnInfo = new ColumnInfo() {MemberInfo = mi};
            var key = PocoColumn.GenerateKey(memberInfos.Concat(new[] { mi }));

            bool explicitColumns = typeConfig.ExplicitColumns ?? false;
            var isColumnDefined = typeConfig.ColumnConfiguration.ContainsKey(key);

            if (isColumnDefined && typeConfig.ColumnConfiguration[key].IsComplexMapping.HasValue && typeConfig.ColumnConfiguration[key].IsComplexMapping.Value)
            {
                columnInfo.ComplexMapping = true;

                if (typeConfig.ColumnConfiguration[key].ComplexPrefix != null)
                    columnInfo.ComplexPrefix = typeConfig.ColumnConfiguration[key].ComplexPrefix;
            }
            else if (isColumnDefined && typeConfig.ColumnConfiguration[key].IsReferenceMember.HasValue && typeConfig.ColumnConfiguration[key].IsReferenceMember.Value)
            {
                if (typeConfig.ColumnConfiguration[key].ReferenceType != null)
                    columnInfo.ReferenceType = typeConfig.ColumnConfiguration[key].ReferenceType.Value;

                if (typeConfig.ColumnConfiguration[key].ReferenceMember != null)
                    columnInfo.ReferenceMemberName = typeConfig.ColumnConfiguration[key].ReferenceMember.Name;
            }
            else if (isColumnDefined && typeConfig.ColumnConfiguration[key].Serialized.HasValue && typeConfig.ColumnConfiguration[key].Serialized.Value)
            {
                columnInfo.Serialized = true;
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
            }
            else
            {
                columnInfo.IgnoreColumn = true;
            }
            
            return columnInfo;
        }
    }
}
