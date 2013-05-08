using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace NPoco.FluentMappings
{
    public class ColumnConfigurationBuilder<T>
    {
        private readonly Dictionary<string, ColumnDefinition> _columnDefinitions;

        public ColumnConfigurationBuilder(Dictionary<string, ColumnDefinition> columnDefinitions)
        {
            _columnDefinitions = columnDefinitions;
        }

        public IColumnBuilder Column(Expression<Func<T, object>> property)
        {
            var propertyInfo = PropertyHelper<T>.GetProperty(property);
            var columnDefinition = new ColumnDefinition() { MemberInfo = propertyInfo };
            var builder = new ColumnBuilder(columnDefinition);
            _columnDefinitions[propertyInfo.Name] = columnDefinition;
            return builder;
        }
    }

    public interface IColumnBuilder
    {
        IColumnBuilder WithName(string name);
        IColumnBuilder WithDbType(Type type);
        IColumnBuilder WithDbType<T>();
        IColumnBuilder Version();
        IColumnBuilder Ignore();
        IColumnBuilder Result();
    }

    public class ColumnBuilder : IColumnBuilder
    {
        private readonly ColumnDefinition _columnDefinition;

        public ColumnBuilder(ColumnDefinition columnDefinition)
        {
            _columnDefinition = columnDefinition;
        }

        public IColumnBuilder WithName(string name)
        {
            _columnDefinition.DbColumnName = name;
            return this;
        }

        public IColumnBuilder WithDbType(Type type)
        {
            _columnDefinition.DbColumnType = type;
            return this;
        }

        public IColumnBuilder WithDbType<T>()
        {
            return WithDbType(typeof (T));
        }

        public IColumnBuilder Version()
        {
            _columnDefinition.VersionColumn = true;
            return this;
        }

        public IColumnBuilder Ignore()
        {
            _columnDefinition.IgnoreColumn = true;
            return this;
        }

        public IColumnBuilder Result()
        {
            _columnDefinition.ResultColumn = true;
            return this;
        }
    }
}