using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NPoco.FluentMappings
{
    public class FluentMappingsPocoData : PocoData
    {
        public FluentMappingsPocoData(Type t, TypeDefinition typeConfig, IMapper mapper, Cache<string, Type> aliasCache)
        {
            AliasToType = aliasCache;
            Mapper = mapper;
            type = t;
            TableInfo = new TableInfo();

            // Get the table name
            var a = typeConfig.TableName ?? "";
            TableInfo.TableName = a.Length == 0 ? t.Name : a;

            // Get the primary key
            a = typeConfig.PrimaryKey ?? "";
            TableInfo.PrimaryKey = a.Length == 0 ? "ID" : a;

            a = typeConfig.SequenceName ?? "";
            TableInfo.SequenceName = a.Length == 0 ? null : a;

            TableInfo.AutoIncrement = typeConfig.AutoIncrement ?? true;

            // Set autoincrement false if primary key has multiple columns
            TableInfo.AutoIncrement = TableInfo.AutoIncrement ? !TableInfo.PrimaryKey.Contains(',') : TableInfo.AutoIncrement;

            // Call column mapper
            if (mapper != null)
                mapper.GetTableInfo(t, TableInfo);

            var alias = CreateAlias(type.Name, type);
            TableInfo.AutoAlias = alias;
            var index = 0;

            // Work out bound properties
            bool explicitColumns = typeConfig.ExplicitColumns ?? false;
            Columns = new Dictionary<string, PocoColumn>(StringComparer.OrdinalIgnoreCase);
            var originalPK = TableInfo.PrimaryKey.Split(',');
            foreach (var mi in ReflectionUtils.GetFieldsAndPropertiesForClasses(t))
            {
                // Work out if properties is to be included
                var isColumnDefined = typeConfig.ColumnConfiguration.ContainsKey(mi.Name);
                if (explicitColumns && !isColumnDefined) continue;

                if (isColumnDefined && (typeConfig.ColumnConfiguration[mi.Name].IgnoreColumn.HasValue && typeConfig.ColumnConfiguration[mi.Name].IgnoreColumn.Value))
                    continue;
                
                var pc = new PocoColumn();
                pc.TableInfo = TableInfo;
                pc.MemberInfo = mi;
                pc.AutoAlias = alias + "_" + index++;
                
                // Work out the DB column name
                if (isColumnDefined)
                {
                    var colattr = typeConfig.ColumnConfiguration[mi.Name];
                    pc.ColumnName = colattr.DbColumnName;
                    pc.ColumnAlias = colattr.DbColumnAlias;
                    if (colattr.ResultColumn.HasValue && colattr.ResultColumn.Value)
                        pc.ResultColumn = true;
                    else if (colattr.VersionColumn.HasValue && colattr.VersionColumn.Value)
                    {
                        pc.VersionColumn = true;
                        pc.VersionColumnType = colattr.VersionColumnType ?? VersionColumnType.Number;
                    }
                    else if (colattr.ComputedColumn.HasValue && colattr.ComputedColumn.Value)
                        pc.ComputedColumn = true;

                    if (colattr.ForceUtc.HasValue && colattr.ForceUtc.Value)
                        pc.ForceToUtc = true;

                    for (int i = 0; i < originalPK.Length; i++)
                    {
                        if (originalPK[i].Equals(mi.Name, StringComparison.OrdinalIgnoreCase))
                            originalPK[i] = (pc.ColumnName ?? mi.Name);
                    }

                    pc.ColumnType = colattr.DbColumnType;
                }
                if (pc.ColumnName == null)
                {
                    pc.ColumnName = mi.Name;
                    if (mapper != null && !mapper.MapMemberToColumn(mi, ref pc.ColumnName, ref pc.ResultColumn))
                        continue;
                }
                
                // Store it
                if (!Columns.ContainsKey(pc.ColumnName))
                    Columns.Add(pc.ColumnName, pc);
            }

            // Recombine the primary key
            TableInfo.PrimaryKey = String.Join(",", originalPK);

            // Build column list for automatic select
            QueryColumns = Columns.Where(x => !x.Value.ResultColumn).ToArray();

        }
    }
}
