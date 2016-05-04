using System;
using System.Linq;
using System.Linq.Expressions;

namespace NPoco.FluentMappings
{
    public class Map<T> : IMap
    {
        private readonly TypeDefinition _petaPocoTypeDefinition;

        public Map() : this(new TypeDefinition(typeof(T)))
        {
        }

        public Map(TypeDefinition petaPocoTypeDefinition)
        {
            _petaPocoTypeDefinition = petaPocoTypeDefinition;
        }

        public void UseMap<TMap>() where TMap : IMap
        {
            Activator.CreateInstance(typeof (TMap), _petaPocoTypeDefinition);

            var keys = _petaPocoTypeDefinition.ColumnConfiguration.Select(x => x.Key).ToList();
            var fieldsAndPropertiesForClasses = ReflectionUtils.GetFieldsAndPropertiesForClasses(typeof(T));
            
            foreach (var key in keys.Where(key => fieldsAndPropertiesForClasses.All(x => x.Name != key)))
            {
                _petaPocoTypeDefinition.ColumnConfiguration.Remove(key);
            }
        }

        public Map<T> TableName(string tableName)
        {
            _petaPocoTypeDefinition.TableName = tableName;
            return this;
        }

        public Map<T> Columns(Action<ColumnConfigurationBuilder<T>> columnConfiguration)
        {
            return Columns(columnConfiguration, null);
        }

        public Map<T> Columns(Action<ColumnConfigurationBuilder<T>> columnConfiguration, bool? explicitColumns)
        {
            _petaPocoTypeDefinition.ExplicitColumns = explicitColumns;
            columnConfiguration(new ColumnConfigurationBuilder<T>(_petaPocoTypeDefinition.ColumnConfiguration));
            return this;
        }

        public Map<T> PrimaryKey(Expression<Func<T, object>> column, string sequenceName)
        {
            var members = MemberHelper<T>.GetMembers(column);
            return PrimaryKey(members.Last().Name, sequenceName);
        }

        public Map<T> PrimaryKey(Expression<Func<T, object>> column)
        {
            _petaPocoTypeDefinition.AutoIncrement = true;
            return PrimaryKey(column, null);
        }

        public Map<T> PrimaryKey(Expression<Func<T, object>> column, bool autoIncrement)
        {
            var members = MemberHelper<T>.GetMembers(column);
            return PrimaryKey(members.Last().Name, autoIncrement);
        }

        public Map<T> CompositePrimaryKey(params Expression<Func<T, object>>[] columns)
        {
            var columnNames = new string[columns.Length];
            for (int i = 0; i < columns.Length; i++)
            {
                columnNames[i] = MemberHelper<T>.GetMembers(columns[i]).Last().Name;
            }

            _petaPocoTypeDefinition.PrimaryKey = string.Join(",", columnNames);
            return this;
        }

        public Map<T> PrimaryKey(string primaryKeyColumn, bool autoIncrement)
        {
            _petaPocoTypeDefinition.PrimaryKey = primaryKeyColumn;
            _petaPocoTypeDefinition.AutoIncrement = autoIncrement;
            return this;
        }

        public Map<T> PrimaryKey(string primaryKeyColumn, bool autoIncrement, bool useOutputClause)
        {
            _petaPocoTypeDefinition.PrimaryKey = primaryKeyColumn;
            _petaPocoTypeDefinition.AutoIncrement = autoIncrement;
            _petaPocoTypeDefinition.UseOutputClause = useOutputClause;
            return this;
        }

        public Map<T> PrimaryKey(string primaryKeyColumn, string sequenceName)
        {
            _petaPocoTypeDefinition.PrimaryKey = primaryKeyColumn;
            _petaPocoTypeDefinition.SequenceName = sequenceName;
            return this;
        }

        public Map<T> PrimaryKey(string primaryKeyColumn, string sequenceName, bool useOutputClause)
        {
            _petaPocoTypeDefinition.PrimaryKey = primaryKeyColumn;
            _petaPocoTypeDefinition.SequenceName = sequenceName;
            _petaPocoTypeDefinition.UseOutputClause = useOutputClause;
            return this;
        }

        public Map<T> PrimaryKey(string primaryKeyColumn)
        {
            return PrimaryKey(primaryKeyColumn, null);
        }

        public Map<T> PersistedType<TPersistedType>()
        {
            return PersistedType(typeof (TPersistedType));
        }
        
        public Map<T> PersistedType(Type type)
        {
            _petaPocoTypeDefinition.PersistedType = type;
            return this;
        }   

        TypeDefinition IMap.TypeDefinition
        {
            get { return _petaPocoTypeDefinition; }
        }
    }
}