using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;

namespace NPoco.RowMappers
{
    public class ValueTupleRowMapper : IRowMapper
    {
        private Func<DbDataReader, object> mapper = default!;
        private MapperCollection mappers;

        private static Cache<(Type, MapperCollection), Func<DbDataReader, object>> cache
            = new Cache<(Type, MapperCollection), Func<DbDataReader, object>>();

        public ValueTupleRowMapper(MapperCollection mappers)
        {
            this.mappers = mappers;
        }

        public void Init(DbDataReader dataReader, PocoData pocoData)
        {
            mapper = GetRowMapper(pocoData.Type, this.mappers, dataReader);
        }

        public object Map(DbDataReader dataReader, RowMapperContext context)
        {
            return mapper(dataReader);
        }

        public static bool IsValueTuple(Type type)
        {
            if (!type.IsGenericType)
                return false;

            var baseType = type.GetGenericTypeDefinition();
            return (
                baseType == typeof(ValueTuple<>) ||
                baseType == typeof(ValueTuple<,>) ||
                baseType == typeof(ValueTuple<,,>) ||
                baseType == typeof(ValueTuple<,,,>) ||
                baseType == typeof(ValueTuple<,,,,>) ||
                baseType == typeof(ValueTuple<,,,,,>) ||
                baseType == typeof(ValueTuple<,,,,,,>) ||
                baseType == typeof(ValueTuple<,,,,,,,>)
            );
        }

        public bool ShouldMap(PocoData pocoData)
        {
            return IsValueTuple(pocoData.Type);
        }

        private static Func<DbDataReader, object> GetRowMapper(Type type, MapperCollection mappers, DbDataReader dataReader)
        {
            return cache.Get((type, mappers), () => CreateRowMapper(type, mappers, dataReader));
        }

        private static Func<DbDataReader, object> CreateRowMapper(Type type, MapperCollection mappers, DbDataReader dataReader)
        {
            var reader = Expression.Parameter(typeof(DbDataReader), "reader");
            var (tupleExpr, _) = CreateTupleExpression(type, mappers, dataReader, reader, 0);

            // reader => (object)new ValueTuple<T1, T2, ...>(value1, value2, ...);
            var expr = Expression.Lambda(
                Expression.Convert(tupleExpr, typeof(object)),
                new[] { reader }
            );
            return (Func<DbDataReader, object>)expr.Compile();
        }

        private static (NewExpression expr, int fieldsIndex) CreateTupleExpression(Type type, MapperCollection mappers, DbDataReader dataReader, ParameterExpression reader, int fieldIndex)
        {
            var argTypes = type.GetGenericArguments();
            var ctor = type.GetConstructor(argTypes);
            var getValue = typeof(DbDataReader).GetMethod("GetValue")!;
            var isDBNull = typeof(DbDataReader).GetMethod("IsDBNull")!;

            if (argTypes.Count() > dataReader.FieldCount)
                throw new InvalidOperationException("SQL query does not return enough fields to fill the tuple");

            var args = new List<Expression>();

            foreach (var argType in argTypes)
            {
                if (IsValueTuple(argType))
                {
                    // It's tuples all the way down
                    var (expr, newFieldIndex) = CreateTupleExpression(argType, mappers, dataReader, reader, fieldIndex);
                    args.Add(expr);
                    fieldIndex += newFieldIndex;
                }
                else
                {
                    if (fieldIndex >= dataReader.FieldCount)
                        throw new InvalidOperationException($"SQL query does not return enough fields to fill the tuple (missing type: {argType.FullName})");

                    var rawValue = Expression.Call(reader, getValue, new[] { Expression.Constant(fieldIndex) });
                    var converter = MappingHelper.GetConverter(mappers, null, dataReader.GetFieldType(fieldIndex), argType);

                    // reader.IsDBNull(i) ? (T)null : converter(reader.GetValue(i))
                    args.Add(Expression.Condition(
                        Expression.Call(reader, isDBNull, new[] { Expression.Constant(fieldIndex) }),
                        Expression.Convert(Expression.Constant(null), argType),
                        Expression.Convert(
                            converter != null
                                ? (Expression)Expression.Invoke(Expression.Constant(converter), new[] { rawValue })
                                : (Expression)rawValue,
                            argType
                        )
                    ));

                    fieldIndex++;
                }
            }

            return (Expression.New(ctor, args), fieldIndex);
        }
    }
}
