using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace NPoco.FluentMappings
{
    public class ColumnConfigurationBuilder<T>
    {
        private readonly Dictionary<string, ColumnDefinition> _columnDefinitions;

        public ColumnConfigurationBuilder(Dictionary<string, ColumnDefinition> columnDefinitions)
        {
            _columnDefinitions = columnDefinitions;
        }

        public void Column(Expression<Func<T, object>> property)
        {
            Column(property, null);
        }

        public void Column(Expression<Func<T, object>> property, string dbColumnName)
        {
            SetColumnDefinition(property, dbColumnName, null, null, null);
        }

        public void Result(Expression<Func<T, object>> property)
        {
            Result(property, null);
        }

        public void Result(Expression<Func<T, object>> property, string dbColumnName)
        {
            SetColumnDefinition(property, dbColumnName, null, true, null);
        }

        public void Ignore(Expression<Func<T, object>> property)
        {
            SetColumnDefinition(property, null, true, null, null);
        }

        public void Version(Expression<Func<T, object>> property)
        {
            Version(property, null);
        }

        public void Version(Expression<Func<T, object>> property, string dbColumnName)
        {
            SetColumnDefinition(property, dbColumnName, null, null, true);
        }

        private void SetColumnDefinition(Expression<Func<T, object>> property, string dbColumnName, bool? ignoreColumn, bool? resultColumn, bool? versionColumn) 
        {
            var propertyInfo = PropertyHelper<T>.GetProperty(property);
            _columnDefinitions[propertyInfo.Name] = new ColumnDefinition
            {
                PropertyInfo = propertyInfo, 
                DbColumnName = dbColumnName,
                ResultColumn = resultColumn,
                IgnoreColumn = ignoreColumn,
                VersionColumn = versionColumn
            };
        }
    }
}