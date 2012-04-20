using System;
using System.Linq.Expressions;

namespace NPoco.FluentMappings
{
    public class PetaPocoMap<T> : IPetaPocoMap
    {
        private readonly PetaPocoTypeDefinition _petaPocoTypeDefinition;

        public PetaPocoMap() : this(new PetaPocoTypeDefinition(typeof(T)))
        {
        }

        public PetaPocoMap(PetaPocoTypeDefinition petaPocoTypeDefinition)
        {
            _petaPocoTypeDefinition = petaPocoTypeDefinition;
        }

        public PetaPocoMap<T> TableName(string tableName)
        {
            _petaPocoTypeDefinition.TableName = tableName;
            return this;
        }

        public PetaPocoMap<T> Columns(Action<PetaPocoColumnConfigurationBuilder<T>> columnConfiguration)
        {
            return Columns(columnConfiguration, null);
        }

        public PetaPocoMap<T> Columns(Action<PetaPocoColumnConfigurationBuilder<T>> columnConfiguration, bool? explicitColumns)
        {
            _petaPocoTypeDefinition.ExplicitColumns = explicitColumns;
            columnConfiguration(new PetaPocoColumnConfigurationBuilder<T>(_petaPocoTypeDefinition.ColumnConfiguration));
            return this;
        }

        public PetaPocoMap<T> PrimaryKey(Expression<Func<T, object>> column, string sequenceName)
        {
            var propertyInfo = PropertyHelper<T>.GetProperty(column);
            return PrimaryKey(propertyInfo.Name, sequenceName);
        }

        public PetaPocoMap<T> PrimaryKey(Expression<Func<T, object>> column)
        {
            _petaPocoTypeDefinition.AutoIncrement = true;
            return PrimaryKey(column, null);
        }

        public PetaPocoMap<T> PrimaryKey(Expression<Func<T, object>> column, bool autoIncrement)
        {
            var propertyInfo = PropertyHelper<T>.GetProperty(column);
            return PrimaryKey(propertyInfo.Name, autoIncrement);
        }

        public PetaPocoMap<T> CompositePrimaryKey(params Expression<Func<T, object>>[] columns)
        {
            var columnNames = new string[columns.Length];
            for (int i = 0; i < columns.Length; i++)
            {
                columnNames[i] = PropertyHelper<T>.GetProperty(columns[i]).Name;
            }

            _petaPocoTypeDefinition.PrimaryKey = string.Join(",", columnNames);
            return this;
        }

        public PetaPocoMap<T> PrimaryKey(string primaryKeyColumn, bool autoIncrement)
        {
            _petaPocoTypeDefinition.PrimaryKey = primaryKeyColumn;
            _petaPocoTypeDefinition.AutoIncrement = autoIncrement;
            return this;
        }

        public PetaPocoMap<T> PrimaryKey(string primaryKeyColumn, string sequenceName)
        {
            _petaPocoTypeDefinition.PrimaryKey = primaryKeyColumn;
            _petaPocoTypeDefinition.SequenceName = sequenceName;
            return this;
        }

        public PetaPocoMap<T> PrimaryKey(string primaryKeyColumn)
        {
            return PrimaryKey(primaryKeyColumn, null);
        }

        PetaPocoTypeDefinition IPetaPocoMap.TypeDefinition
        {
            get { return _petaPocoTypeDefinition; }
        }
    }
}