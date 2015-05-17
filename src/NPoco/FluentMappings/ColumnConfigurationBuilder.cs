using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
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

        public IColumnBuilder<T2> Column<T2>(Expression<Func<T, T2>> property)
        {
            var members = MemberHelper<T>.GetMembers(property);
            var columnDefinition = new ColumnDefinition() { MemberInfo = members.Last() };
            var builder = new ColumnBuilder<T2>(columnDefinition);
            var key = string.Join("__", members.Select(x => x.Name));
            _columnDefinitions[key] = columnDefinition;
            return builder;
        }
    }

    public interface IColumnBuilder<TModel>
    {
        IColumnBuilder<TModel> WithName(string name);
        IColumnBuilder<TModel> WithAlias(string alias);
        IColumnBuilder<TModel> WithDbType(Type type);
        IColumnBuilder<TModel> WithDbType<T>();
        IColumnBuilder<TModel> Version();
        IColumnBuilder<TModel> Ignore();
        IColumnBuilder<TModel> Result();
        IColumnBuilder<TModel> Computed();
        IColumnBuilder<TModel> Reference();
        IColumnBuilder<TModel> Reference(Expression<Func<TModel, object>> member);
    }

    public class ColumnBuilder<TModel> : IColumnBuilder<TModel>
    {
        private readonly ColumnDefinition _columnDefinition;

        public ColumnBuilder(ColumnDefinition columnDefinition)
        {
            _columnDefinition = columnDefinition;
        }

        public IColumnBuilder<TModel> WithName(string name)
        {
            _columnDefinition.DbColumnName = name;
            return this;
        }

        public IColumnBuilder<TModel> WithAlias(string alias)
        {
            _columnDefinition.DbColumnAlias = alias;
            return this;
        }

        public IColumnBuilder<TModel> WithDbType(Type type)
        {
            _columnDefinition.DbColumnType = type;
            return this;
        }

        public IColumnBuilder<TModel> WithDbType<T>()
        {
            return WithDbType(typeof (T));
        }

        public IColumnBuilder<TModel> Version()
        {
            _columnDefinition.VersionColumn = true;
            return this;
        }

        public IColumnBuilder<TModel> Version(VersionColumnType versionColumnType)
        {
            _columnDefinition.VersionColumn = true;
            _columnDefinition.VersionColumnType = versionColumnType;
            return this;
        }

        public IColumnBuilder<TModel> Ignore()
        {
            _columnDefinition.IgnoreColumn = true;
            return this;
        }

        public IColumnBuilder<TModel> Result()
        {
            _columnDefinition.ResultColumn = true;
            return this;
        }

        public IColumnBuilder<TModel> Computed()
        {
            _columnDefinition.ComputedColumn = true;
            return this;
        }

        public IColumnBuilder<TModel> Reference()
        {
            _columnDefinition.IsReferenceMember = true;
            return this;
        }

        public IColumnBuilder<TModel> Reference(Expression<Func<TModel, object>> joinColumn)
        {
            _columnDefinition.IsReferenceMember = true;
            _columnDefinition.ReferenceMember = MemberHelper<TModel>.GetMembers(joinColumn).Last();
            return this;
        }
    }
}