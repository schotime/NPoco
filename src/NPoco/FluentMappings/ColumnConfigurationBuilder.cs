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
            var memberInfo = members.Last();
            var columnDefinition = new ColumnDefinition() { MemberInfo = memberInfo };
            var builder = new ColumnBuilder<T2>(columnDefinition);
            var key = PocoColumn.GenerateKey(members);
            _columnDefinitions[key] = columnDefinition;
            return builder;
        }

        public IColumnBuilder<T2> Column<T2>(string privateProperty)
        {
            var memberInfo = ReflectionUtils.GetPrivatePropertiesForClasses(typeof(T)).FirstOrDefault(x => x.Name == privateProperty);
            if (memberInfo is null)
            {
                throw new Exception($"The name of the private property '{privateProperty}' does not exist of the type '{typeof(T)}'");
            }
            var columnDefinition = new ColumnDefinition() { MemberInfo = memberInfo };
            if (memberInfo.GetMemberInfoType() != typeof(T2))
            {
                throw new Exception($"The type of the property '{memberInfo.GetMemberInfoType()}' doesn't match the generic type provided '{typeof(T2)}'");
            }
            var builder = new ColumnBuilder<T2>(columnDefinition);
            var key = memberInfo.Name;
            _columnDefinitions[key] = columnDefinition;
            return builder;
        }

        public IManyColumnBuilder<T2> Many<T2>(Expression<Func<T, IList<T2>>> property)
        {
            var members = MemberHelper<T>.GetMembers(property);
            var columnDefinition = new ColumnDefinition() { MemberInfo = members.Last() };
            var builder = new ManyColumnBuilder<T2>(columnDefinition);
            var key = PocoColumn.GenerateKey(members);
            _columnDefinitions[key] = columnDefinition;
            return builder;
        }
    }

    public interface IManyColumnBuilder<TModel>
    {
        IManyColumnBuilder<TModel> WithName(string name);
        IManyColumnBuilder<TModel> WithDbType(Type type);
        IManyColumnBuilder<TModel> WithDbType<T>();
        IManyColumnBuilder<TModel> Reference(Expression<Func<TModel, object>> member);
        IManyColumnBuilder<TModel> LimitMappingDepth(int limit = 1);        
    }

    public class ManyColumnBuilder<TModel> : IManyColumnBuilder<TModel>
    {
        private readonly ColumnDefinition _columnDefinition;

        public ManyColumnBuilder(ColumnDefinition columnDefinition)
        {
            _columnDefinition = columnDefinition;
        }

        public IManyColumnBuilder<TModel> WithName(string name)
        {
            _columnDefinition.DbColumnName = name;
            return this;
        }

        public IManyColumnBuilder<TModel> WithDbType(Type type)
        {
            _columnDefinition.DbColumnType = type;
            return this;
        }

        public IManyColumnBuilder<TModel> WithDbType<T>()
        {
            return WithDbType(typeof(T));
        }

        public IManyColumnBuilder<TModel> Reference(Expression<Func<TModel, object>> member)
        {
            _columnDefinition.IsReferenceMember = true;
            _columnDefinition.ReferenceType = ReferenceType.Many;
            _columnDefinition.ReferenceMember = MemberHelper<TModel>.GetMembers(member).Last();
            return this;
        }
        /// <summary>
        /// Sets the hard mapping depth limit for this column's definition
        /// </summary>
        /// <param name="limit">integer hard depth limit. Defaults to and can't be less than 1</param>
        /// <returns>The ManyColumnBuilder</returns>
        public IManyColumnBuilder<TModel> LimitMappingDepth(int limit = 1)
        {
            if ( limit < 1 )
                throw new ArgumentOutOfRangeException("limit", "The mapping depth limit must always be at least equal to 1");

                _columnDefinition.HardDepthLimit = limit;

                return this;
        }
    }

    public interface IColumnBuilder<TModel>
    {
        IColumnBuilder<TModel> WithName(string name);
        IColumnBuilder<TModel> WithName(string name, bool exactMatch);
        IColumnBuilder<TModel> WithAlias(string alias);
        IColumnBuilder<TModel> WithDbType(Type type);
        IColumnBuilder<TModel> WithDbType<T>();
        IColumnBuilder<TModel> Version();
        IColumnBuilder<TModel> Version(VersionColumnType versionColumnType);
        IColumnBuilder<TModel> Ignore();
        IColumnBuilder<TModel> Result();
        IColumnBuilder<TModel> Computed();
        IColumnBuilder<TModel> Computed(ComputedColumnType computedColumnType);
        IColumnBuilder<TModel> Reference(ReferenceType referenceType = ReferenceType.Foreign);
        IColumnBuilder<TModel> Reference(Expression<Func<TModel, object>> member, ReferenceType referenceType = ReferenceType.Foreign);
        IColumnBuilder<TModel> Serialized();
        IColumnBuilder<TModel> ComplexMapping(string prefix = null);
        IColumnBuilder<TModel> ValueObject();
        IColumnBuilder<TModel> ValueObject(Expression<Func<TModel, object>> member);
        IColumnBuilder<TModel> ForceToUtc(bool enabled);
        IColumnBuilder<TModel> LimitMappingDepth(int limit = 1);
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

        public IColumnBuilder<TModel> WithName(string name, bool exactMatch)
        {
            _columnDefinition.DbColumnName = name;
            _columnDefinition.ExactColumnNameMatch = exactMatch;
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

        public IColumnBuilder<TModel> Computed(ComputedColumnType computedColumnType)
        {
            _columnDefinition.ComputedColumnType = computedColumnType;
            return this;
        }

        public IColumnBuilder<TModel> Reference(ReferenceType referenceType = ReferenceType.Foreign)
        {
            if (referenceType == ReferenceType.Many)
            {
                throw new Exception("Use Many(x => x.Items) instead of Column(x => x.Items) for one to many relationships");
            }

            _columnDefinition.IsReferenceMember = true;
            _columnDefinition.ReferenceType = referenceType;
            return this;
        }

        public IColumnBuilder<TModel> Reference(Expression<Func<TModel, object>> member, ReferenceType referenceType = ReferenceType.Foreign)
        {
            Reference(referenceType);
            _columnDefinition.ReferenceMember = MemberHelper<TModel>.GetMembers(member).Last();
            return this;
        }

        public IColumnBuilder<TModel> Serialized()
        {
            _columnDefinition.Serialized = true;
            return this;
        }

        public IColumnBuilder<TModel> ComplexMapping(string prefix = null)
        {
            _columnDefinition.IsComplexMapping = true;
            _columnDefinition.ComplexPrefix = prefix;
            return this;
        }

        public IColumnBuilder<TModel> ValueObject()
        {
            _columnDefinition.ValueObjectColumn = true;
            return this;
        }

        public IColumnBuilder<TModel> ValueObject(string name)
        {
            ValueObject();
            _columnDefinition.ValueObjectColumnName = name;
            return this;
        }

        public IColumnBuilder<TModel> ValueObject(Expression<Func<TModel, object>> member)
        {
            ValueObject();
            ValueObject(MemberHelper<TModel>.GetMembers(member).Last().Name);
            return this;
        }

        public IColumnBuilder<TModel> ForceToUtc(bool enabled)
        {
            _columnDefinition.ForceUtc = enabled;
            return this;
        }
        /// <summary>
        /// Sets the hard mapping depth limit for this column's definition
        /// </summary>
        /// <param name="limit">integer hard depth limit. Defaults to and can't be less than 1</param>
        /// <returns>The ColumnBuilder</returns>
        public IColumnBuilder<TModel> LimitMappingDepth(int limit = 1)
        {
            if ( limit < 1 )
                throw new ArgumentOutOfRangeException("limit", "The mapping depth limit must always be at least equal to 1");

                _columnDefinition.HardDepthLimit = limit;

            return this;
        }        
    }
}