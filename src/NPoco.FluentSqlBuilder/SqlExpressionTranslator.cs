using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace NPoco.FluentSqlBuilder
{
    internal sealed class SqlExpressionTranslator
    {
        private readonly IDatabase _database;
        private readonly IList<object> _parameters;
        private readonly Dictionary<ParameterExpression, TableReference> _tables;
        private readonly HashSet<TableReference> _availableTables;

        internal SqlExpressionTranslator(IDatabase database, IList<object> parameters, LambdaExpression expression, IList<TableReference> tables)
        {
            _database = database;
            _parameters = parameters;
            _tables = new Dictionary<ParameterExpression, TableReference>();
            _availableTables = new HashSet<TableReference>(tables);
            if (expression.Parameters.Count > tables.Count)
                throw new ArgumentException("Expression has more parameters than available table references.", nameof(expression));
            if (expression.Parameters.Count == 1)
            {
                var parameter = expression.Parameters[0];
                var match = tables.LastOrDefault(x => x.EntityType == parameter.Type);
                if (match == null) throw new ArgumentException("Expression parameter type does not match an available table reference.", nameof(expression));
                _tables.Add(parameter, match);
            }
            else
            {
                for (var i = 0; i < expression.Parameters.Count; i++) _tables.Add(expression.Parameters[i], tables[i]);
            }
        }

        internal string Translate(Expression expression) => Visit(StripConvert(expression));
        internal string TranslatePredicate(Expression expression) => Predicate(StripConvert(expression));

        internal IList<string> TranslateList(Expression expression)
        {
            expression = StripConvert(expression);
            var created = expression as NewExpression;
            if (created != null) return created.Arguments.SelectMany(TranslateList).ToList();
            var initialized = expression as MemberInitExpression;
            if (initialized != null) return initialized.Bindings.Cast<MemberAssignment>().SelectMany(x => TranslateList(x.Expression)).ToList();
            return new[] { Translate(expression) };
        }

        private string Visit(Expression expression)
        {
            expression = StripConvert(expression);
            switch (expression.NodeType)
            {
                case ExpressionType.AndAlso: return BooleanBinary((BinaryExpression)expression, "AND");
                case ExpressionType.OrElse: return BooleanBinary((BinaryExpression)expression, "OR");
                case ExpressionType.Equal: return Comparison((BinaryExpression)expression, "=");
                case ExpressionType.NotEqual: return Comparison((BinaryExpression)expression, "<>");
                case ExpressionType.GreaterThan: return Binary((BinaryExpression)expression, ">");
                case ExpressionType.GreaterThanOrEqual: return Binary((BinaryExpression)expression, ">=");
                case ExpressionType.LessThan: return Binary((BinaryExpression)expression, "<");
                case ExpressionType.LessThanOrEqual: return Binary((BinaryExpression)expression, "<=");
                case ExpressionType.Add:
                    var add = (BinaryExpression)expression;
                    return add.Type == typeof(string) ? Concatenate(add) : Binary(add, "+");
                case ExpressionType.Subtract: return Binary((BinaryExpression)expression, "-");
                case ExpressionType.Multiply: return Binary((BinaryExpression)expression, "*");
                case ExpressionType.Divide: return Binary((BinaryExpression)expression, "/");
                case ExpressionType.Modulo: return Binary((BinaryExpression)expression, "%");
                case ExpressionType.And: return Binary((BinaryExpression)expression, "&");
                case ExpressionType.Or: return Binary((BinaryExpression)expression, "|");
                case ExpressionType.ExclusiveOr: return Binary((BinaryExpression)expression, "^");
                case ExpressionType.Not: return Negate((UnaryExpression)expression);
                case ExpressionType.MemberAccess: return Member((MemberExpression)expression);
                case ExpressionType.Call: return MethodCall((MethodCallExpression)expression);
                case ExpressionType.Constant: return Parameter(((ConstantExpression)expression).Value);
                case ExpressionType.Coalesce: return "COALESCE(" + Visit(((BinaryExpression)expression).Left) + ", " + Visit(((BinaryExpression)expression).Right) + ")";
                case ExpressionType.Conditional:
                    var conditional = (ConditionalExpression)expression;
                    return "(CASE WHEN " + Predicate(conditional.Test) + " THEN " + Visit(conditional.IfTrue) + " ELSE " + Visit(conditional.IfFalse) + " END)";
                default:
                    if (CanEvaluate(expression)) return Parameter(Evaluate(expression));
                    throw new NotSupportedException("Expression node '" + expression.NodeType + "' is not supported by the fluent SQL builder.");
            }
        }

        private string Binary(BinaryExpression expression, string operation)
            => "(" + Visit(expression.Left) + " " + operation + " " + Visit(expression.Right) + ")";

        private string BooleanBinary(BinaryExpression expression, string operation)
            => "(" + Predicate(expression.Left) + " " + operation + " " + Predicate(expression.Right) + ")";

        private string Concatenate(BinaryExpression expression)
        {
            var provider = (_database.DatabaseType.GetProviderName() ?? string.Empty).ToLowerInvariant();
            if (provider.Contains("mysql")) return "CONCAT(" + Visit(expression.Left) + ", " + Visit(expression.Right) + ")";
            return "(" + Visit(expression.Left) + (IsSqlServer(provider) ? " + " : " || ") + Visit(expression.Right) + ")";
        }

        private string Predicate(Expression expression)
        {
            expression = StripConvert(expression);
            if (CanEvaluate(expression) && expression.Type == typeof(bool))
                return (bool)Evaluate(expression) ? "(1 = 1)" : "(1 = 0)";
            var member = expression as MemberExpression;
            if (member != null && member.Member.Name == "HasValue" && member.Expression != null && Nullable.GetUnderlyingType(member.Expression.Type) != null)
                return "(" + Visit(member.Expression) + " IS NOT NULL)";
            TableReference table;
            MemberInfo[] chain;
            if (member != null && member.Type == typeof(bool) && TryResolveColumn(member, out table, out chain))
                return "(" + table.GetColumn(chain) + " = " + Parameter(true, table.ResolveColumn(chain)) + ")";
            return Visit(expression);
        }

        private string Negate(UnaryExpression expression)
        {
            var operand = StripConvert(expression.Operand);
            var member = operand as MemberExpression;
            TableReference table;
            MemberInfo[] chain;
            if (member != null && member.Type == typeof(bool) && TryResolveColumn(member, out table, out chain))
                return "(" + table.GetColumn(chain) + " = " + Parameter(false, table.ResolveColumn(chain)) + ")";
            return "(NOT " + Predicate(operand) + ")";
        }

        private string Comparison(BinaryExpression expression, string operation)
        {
            if (IsNull(expression.Right)) return "(" + Visit(expression.Left) + (operation == "=" ? " IS NULL" : " IS NOT NULL") + ")";
            if (IsNull(expression.Left)) return "(" + Visit(expression.Right) + (operation == "=" ? " IS NULL" : " IS NOT NULL") + ")";

            TableReference table;
            MemberInfo[] chain;
            if (TryResolveColumnExpression(expression.Left, out table, out chain) && CanEvaluate(expression.Right))
                return "(" + Visit(expression.Left) + " " + operation + " " + Parameter(Evaluate(expression.Right), table.ResolveColumn(chain)) + ")";
            if (TryResolveColumnExpression(expression.Right, out table, out chain) && CanEvaluate(expression.Left))
                return "(" + Parameter(Evaluate(expression.Left), table.ResolveColumn(chain)) + " " + operation + " " + Visit(expression.Right) + ")";
            return Binary(expression, operation);
        }

        private string Member(MemberExpression expression)
        {
            if (expression.Member.Name == "HasValue" && expression.Expression != null && Nullable.GetUnderlyingType(expression.Expression.Type) != null)
                return "(" + Visit(expression.Expression) + " IS NOT NULL)";
            if (expression.Member.Name == "Value" && expression.Expression != null && Nullable.GetUnderlyingType(expression.Expression.Type) != null)
                return Visit(expression.Expression);

            if (IsDatePart(expression.Member.Name) && expression.Expression != null)
            {
                if (!CanEvaluate(expression.Expression))
                    return DatePart(expression.Member.Name, Visit(expression.Expression));
            }

            TableReference table;
            MemberInfo[] chain;
            if (TryResolveColumn(expression, out table, out chain))
                return table.GetColumn(chain);

            return Parameter(Evaluate(expression));
        }

        private string MethodCall(MethodCallExpression expression)
        {
            if (IsFluentSqlMarker(expression.Method))
            {
                if (expression.Method.Name == "Raw") return RawFragment(expression);
                if (expression.Method.Name == "Scalar") return ScalarSubquery(expression);
            }
            if (expression.Method.DeclaringType == typeof(FluentSqlFunctions))
            {
                if (expression.Method.Name == "Case")
                    return "(CASE WHEN " + Predicate(expression.Arguments[0]) + " THEN " + Visit(expression.Arguments[1]) + " ELSE " + Visit(expression.Arguments[2]) + " END)";
                return Aggregate(expression);
            }
            if (expression.Method.DeclaringType == typeof(FluentSql))
            {
                if (IsAggregate(expression.Method.Name)) return Aggregate(expression);
                if (typeof(IFluentSqlQuery).IsAssignableFrom(expression.Arguments.Last().Type)) return Subquery(expression);
                if (expression.Method.Name == "In" || expression.Method.Name == "NotIn")
                {
                    var contains = CollectionContains(expression.Arguments[1], expression.Arguments[0]);
                    return expression.Method.Name == "NotIn" ? "(NOT " + contains + ")" : contains;
                }
                if (expression.Method.Name == "Case")
                    return "(CASE WHEN " + Predicate(expression.Arguments[0]) + " THEN " + Visit(expression.Arguments[1]) + " ELSE " + Visit(expression.Arguments[2]) + " END)";
            }

            if (expression.Method.DeclaringType == typeof(string) && expression.Method.Name == "IsNullOrEmpty")
            {
                var value = Visit(expression.Arguments[0]);
                return "(" + value + " IS NULL OR " + value + " = " + Parameter(string.Empty) + ")";
            }

            if (expression.Method.DeclaringType == typeof(string) && expression.Method.Name == "Equals" && expression.Arguments.Count >= 2)
                return "(" + Visit(expression.Arguments[0]) + " = " + Visit(expression.Arguments[1]) + ")";

            if (expression.Object != null && expression.Object.Type == typeof(string))
            {
                if (expression.Method.Name == "Contains") return Like(expression.Object, expression.Arguments[0], "%", "%");
                if (expression.Method.Name == "StartsWith") return Like(expression.Object, expression.Arguments[0], string.Empty, "%");
                if (expression.Method.Name == "EndsWith") return Like(expression.Object, expression.Arguments[0], "%", string.Empty);
                if (expression.Method.Name == "ToUpper") return "UPPER(" + Visit(expression.Object) + ")";
                if (expression.Method.Name == "ToLower") return "LOWER(" + Visit(expression.Object) + ")";
                if (expression.Method.Name == "Trim") return "TRIM(" + Visit(expression.Object) + ")";
                if (expression.Method.Name == "TrimStart") return "LTRIM(" + Visit(expression.Object) + ")";
                if (expression.Method.Name == "TrimEnd") return "RTRIM(" + Visit(expression.Object) + ")";
                if (expression.Method.Name == "Equals" && expression.Arguments.Count == 1)
                    return "(" + Visit(expression.Object) + " = " + Visit(expression.Arguments[0]) + ")";
            }

            if (expression.Method.DeclaringType == typeof(string) && expression.Method.Name == "Concat")
            {
                var arguments = expression.Arguments.Count == 1 && expression.Arguments[0] is NewArrayExpression
                    ? ((NewArrayExpression)expression.Arguments[0]).Expressions
                    : expression.Arguments;
                return Concatenate(arguments);
            }

            if (expression.Object != null && (expression.Object.Type == typeof(DateTime) || expression.Object.Type == typeof(DateTime?)))
            {
                if (expression.Method.Name == "AddDays") return DateAdd("day", expression.Object, expression.Arguments[0]);
                if (expression.Method.Name == "AddMonths") return DateAdd("month", expression.Object, expression.Arguments[0]);
                if (expression.Method.Name == "AddYears") return DateAdd("year", expression.Object, expression.Arguments[0]);
                if (expression.Method.Name == "AddHours") return DateAdd("hour", expression.Object, expression.Arguments[0]);
                if (expression.Method.Name == "AddMinutes") return DateAdd("minute", expression.Object, expression.Arguments[0]);
                if (expression.Method.Name == "AddSeconds") return DateAdd("second", expression.Object, expression.Arguments[0]);
            }

            Expression collection = null;
            Expression item = null;
            if (expression.Method.Name == "Contains" && expression.Object != null && expression.Object.Type != typeof(string))
            {
                collection = expression.Object;
                item = expression.Arguments[0];
            }
            else if (expression.Method.Name == "Contains" && expression.Object == null && expression.Arguments.Count >= 2)
            {
                collection = expression.Arguments.Select(FindEnumerableExpression).FirstOrDefault(x => x != null);
                item = expression.Arguments.FirstOrDefault(x => ContainsParameter(x));
            }
            if (collection != null && CanEvaluate(collection)) return CollectionContains(collection, item);

            if (CanEvaluate(expression)) return Parameter(Evaluate(expression));
            throw new NotSupportedException("Method '" + expression.Method.Name + "' is not supported by the fluent SQL builder.");
        }

        private string Aggregate(MethodCallExpression expression)
        {
            if (expression.Method.Name == "CountDistinct") return "COUNT(DISTINCT " + Visit(expression.Arguments[0]) + ")";
            var name = expression.Method.Name == "Average" ? "AVG" : expression.Method.Name.ToUpperInvariant();
            return name + "(" + (expression.Arguments.Count == 0 ? "*" : Visit(expression.Arguments[0])) + ")";
        }

        private static bool IsAggregate(string name)
            => name == "Count" || name == "CountDistinct" || name == "Sum" || name == "Average" || name == "Min" || name == "Max";

        private string Concatenate(IEnumerable<Expression> expressions)
        {
            var values = expressions.Select(Visit).ToArray();
            var provider = (_database.DatabaseType.GetProviderName() ?? string.Empty).ToLowerInvariant();
            if (provider.Contains("mysql")) return "CONCAT(" + string.Join(", ", values) + ")";
            return "(" + string.Join(IsSqlServer(provider) ? " + " : " || ", values) + ")";
        }

        private string DateAdd(string part, Expression value, Expression amount)
        {
            var provider = (_database.DatabaseType.GetProviderName() ?? string.Empty).ToLowerInvariant();
            var column = Visit(value);
            var increment = Visit(amount);
            if (provider.Contains("mysql")) return "DATE_ADD(" + column + ", INTERVAL " + increment + " " + part.ToUpperInvariant() + ")";
            if (IsSqlServer(provider)) return "DATEADD(" + part + ", " + increment + ", " + column + ")";
            if (provider.Contains("sqlite")) return "datetime(" + column + ", " + increment + " || ' " + part + "')";
            if (provider.Contains("npgsql")) return "(" + column + " + (" + increment + " * INTERVAL '1 " + part + "'))";
            if (provider.Contains("oracle"))
            {
                if (part == "month") return "ADD_MONTHS(" + column + ", " + increment + ")";
                if (part == "year") return "ADD_MONTHS(" + column + ", (" + increment + " * 12))";
                if (part == "day") return "(" + column + " + " + increment + ")";
                var divisor = part == "hour" ? "24" : part == "minute" ? "1440" : "86400";
                return "(" + column + " + (" + increment + " / " + divisor + "))";
            }
            if (provider.Contains("firebird")) return "DATEADD(" + part.ToUpperInvariant() + ", " + increment + ", " + column + ")";
            throw new NotSupportedException("Date addition is not supported by this database provider.");
        }

        private static bool IsSqlServer(string provider) => provider.Contains("sqlclient") && !provider.Contains("mysql");

        private static bool IsFluentSqlMarker(MethodInfo method)
            => method.DeclaringType == typeof(FluentSql) || method.DeclaringType == typeof(FluentSqlFunctions);

        private string RawFragment(MethodCallExpression expression)
        {
            if (!CanEvaluate(expression.Arguments[0]))
                throw new NotSupportedException("The SQL text passed to FluentSql.Raw must be a constant or a captured value.");
            var format = Evaluate(expression.Arguments[0]) as string;
            if (string.IsNullOrWhiteSpace(format))
                throw new ArgumentException("FluentSql.Raw requires SQL text.");

            var operands = RawOperands(expression);
            var rendered = new object[operands.Length];
            for (var i = 0; i < operands.Length; i++)
            {
                var lambda = StripConvert(operands[i]) as LambdaExpression;
                if (lambda == null)
                    throw new NotSupportedException("FluentSql.Raw arguments must be lambdas, for example () => table.Row.Column.");
                rendered[i] = Visit(lambda.Body);
            }

            try
            {
                return "(" + string.Format(CultureInfo.InvariantCulture, format, rendered) + ")";
            }
            catch (FormatException exception)
            {
                throw new ArgumentException("The SQL text passed to FluentSql.Raw has a placeholder that does not match its "
                    + rendered.Length + " argument(s). Use {0}, {1}, ... and {{ }} for a literal brace. SQL: " + format, exception);
            }
        }

        private static Expression[] RawOperands(MethodCallExpression expression)
        {
            if (expression.Arguments.Count < 2) return new Expression[0];
            var argument = StripConvert(expression.Arguments[1]);
            var array = argument as NewArrayExpression;
            if (array != null) return array.Expressions.ToArray();
            var values = CanEvaluate(argument) ? Evaluate(argument) as IEnumerable : null;
            if (values == null)
                throw new NotSupportedException("FluentSql.Raw arguments must be lambdas, for example () => table.Row.Column.");
            return values.Cast<Expression>().ToArray();
        }

        private string ScalarSubquery(MethodCallExpression expression)
        {
            var query = Evaluate(expression.Arguments[0]) as IFluentSqlQueryInternal;
            if (query == null)
                throw new InvalidOperationException("FluentSql.Scalar requires a query built by the fluent SQL builder.");
            RequireSingleColumn(query, "FluentSql.Scalar");
            return "(" + query.Build(_parameters) + ")";
        }

        // A subquery standing in for a value has to yield one column. Databases reject the rest,
        // but only once the query runs, and with a message that says nothing about which subquery.
        private static void RequireSingleColumn(IFluentSqlQueryInternal query, string usage)
        {
            var columns = query.ProjectedColumnCount;
            if (columns != 1)
                throw new InvalidOperationException(usage + " requires a subquery projecting exactly one column, but it projects "
                    + columns + ". Use SelectScalar, or Select with a single member.");
        }

        private string Subquery(MethodCallExpression expression)
        {
            var queryExpression = expression.Arguments[expression.Arguments.Count - 1];
            var query = Evaluate(queryExpression) as IFluentSqlQueryInternal;
            if (query == null) throw new InvalidOperationException("A SQL subquery expression requires a FluentSqlQuery instance.");
            if (expression.Method.Name == "In" || expression.Method.Name == "NotIn")
                RequireSingleColumn(query, "FluentSql." + expression.Method.Name);
            var nestedSql = query.Build(_parameters);
            if (expression.Method.Name == "Exists") return "EXISTS (" + nestedSql + ")";
            if (expression.Method.Name == "NotExists") return "NOT EXISTS (" + nestedSql + ")";
            var op = expression.Method.Name == "NotIn" ? " NOT IN " : " IN ";
            return "(" + Visit(expression.Arguments[0]) + op + "(" + nestedSql + ")" + ")";
        }

        private string Like(Expression column, Expression value, string prefix, string suffix)
        {
            if (!CanEvaluate(value)) throw new NotSupportedException("LIKE values must be captured values or constants.");
            var evaluated = Evaluate(value);
            var text = Convert.ToString(evaluated, CultureInfo.InvariantCulture) ?? string.Empty;
            text = text.Replace("!", "!!").Replace("%", "!%").Replace("_", "!_");
            return "(UPPER(" + Visit(column) + ") LIKE " + Parameter(prefix + text.ToUpperInvariant() + suffix) + " ESCAPE '!')";
        }

        private string CollectionContains(Expression collectionExpression, Expression item)
        {
            var values = Evaluate(collectionExpression) as IEnumerable;
            if (values == null) return "(1 = 0)";
            TableReference table;
            MemberInfo[] chain;
            var column = TryResolveColumnExpression(item, out table, out chain) ? table.ResolveColumn(chain) : null;
            var parameters = new List<string>();
            foreach (var value in values)
            {
                var nested = value as IEnumerable;
                if (nested != null && !(value is string) && !(value is byte[]))
                {
                    foreach (var nestedValue in nested) parameters.Add(Parameter(nestedValue, column));
                }
                else
                {
                    parameters.Add(Parameter(value, column));
                }
            }
            if (parameters.Count == 0) return "(1 = 0)";
            return "(" + Visit(item) + " IN (" + string.Join(", ", parameters) + "))";
        }

        private string Parameter(object value, PocoColumn column = null)
        {
            var index = _parameters.Count;
            _parameters.Add(column == null ? value : ConvertParameter(column, value));
            return "@" + index;
        }

        private object ConvertParameter(PocoColumn column, object value)
        {
            if (column.ValueObjectColumn && value != null) value = column.GetValueObjectValue(value);
            var converter = _database.Mappers.FindToDbConverter(column.ColumnType, column.MemberInfoData.MemberInfo);
            if (converter != null) return converter(value);
            if (column.SerializedColumn) return _database.Mappers.ColumnSerializer.Serialize(value);
            if (column.ColumnType == typeof(string) && IsEnum(column.MemberInfoData.MemberType) && value != null) return value.ToString();
            if (column.ColumnType == typeof(AnsiString) && value is string) return new AnsiString((string)value);
            if ((column.MemberInfoData.MemberType == typeof(char) || column.MemberInfoData.MemberType == typeof(char?)) && value is int)
                return Convert.ToChar(value, CultureInfo.InvariantCulture);
            return _database.DatabaseType.ProcessDefaultMappings(column, value);
        }

        private static bool IsEnum(Type type)
        {
            var effective = Nullable.GetUnderlyingType(type) ?? type;
            return effective.GetTypeInfo().IsEnum;
        }

        private static bool IsDatePart(string name)
            => name == "Year" || name == "Month" || name == "Day" || name == "Hour" || name == "Minute" || name == "Second";

        private string DatePart(string name, string column)
        {
            var provider = (_database.DatabaseType.GetProviderName() ?? string.Empty).ToLowerInvariant();
            if (provider.Contains("mysql")) return name.ToUpperInvariant() + "(" + column + ")";
            if (provider.Contains("npgsql") || provider.Contains("firebird")) return "EXTRACT(" + name.ToUpperInvariant() + " FROM " + column + ")";
            if (provider.Contains("oracle")) return "EXTRACT(" + name.ToUpperInvariant() + " FROM CAST(" + column + " AS TIMESTAMP))";
            if (provider.Contains("sqlite"))
            {
                var format = name == "Year" ? "%Y" : name == "Month" ? "%m" : name == "Day" ? "%d" : name == "Hour" ? "%H" : name == "Minute" ? "%M" : "%S";
                return "CAST(strftime('" + format + "', " + column + ") AS INTEGER)";
            }
            return "DATEPART(" + name.ToLowerInvariant() + ", " + column + ")";
        }

        private bool TryResolveColumnExpression(Expression expression, out TableReference table, out MemberInfo[] chain)
        {
            expression = StripConvert(expression);
            var member = expression as MemberExpression;
            if (member != null && member.Member.Name == "Value" && member.Expression != null && Nullable.GetUnderlyingType(member.Expression.Type) != null)
                member = StripConvert(member.Expression) as MemberExpression;
            if (member != null) return TryResolveColumn(member, out table, out chain);
            table = null;
            chain = null;
            return false;
        }

        private bool TryResolveColumn(MemberExpression expression, out TableReference table, out MemberInfo[] chain)
        {
            var members = new List<MemberInfo>();
            Expression current = expression;
            while (current is MemberExpression)
            {
                var member = (MemberExpression)current;
                if (member.Member.Name == "Row" && member.Member.DeclaringType != null && member.Member.DeclaringType.GetTypeInfo().IsGenericType && member.Member.DeclaringType.GetGenericTypeDefinition() == typeof(TableReference<>))
                {
                    var reference = Evaluate(member.Expression) as TableReference;
                    if (reference == null || !_availableTables.Contains(reference))
                        throw new InvalidOperationException("The table reference is not available to this query.");
                    table = reference;
                    chain = members.ToArray();
                    return true;
                }
                members.Insert(0, member.Member);
                current = StripConvert(member.Expression);
            }
            var parameter = current as ParameterExpression;
            if (parameter != null && _tables.TryGetValue(parameter, out table))
            {
                chain = members.ToArray();
                return true;
            }
            table = null;
            chain = null;
            return false;
        }

        private static Expression StripConvert(Expression expression)
        {
            while (expression != null && (expression.NodeType == ExpressionType.Convert || expression.NodeType == ExpressionType.ConvertChecked || expression.NodeType == ExpressionType.Quote))
                expression = ((UnaryExpression)expression).Operand;
            return expression;
        }

        private static bool IsNull(Expression expression) => CanEvaluate(expression) && Evaluate(expression) == null;
        private static bool CanEvaluate(Expression expression) => !ContainsParameter(expression);

        private static Expression FindEnumerableExpression(Expression expression)
        {
            expression = StripConvert(expression);
            if (expression != null && typeof(IEnumerable).IsAssignableFrom(expression.Type) && CanEvaluate(expression)) return expression;
            var call = expression as MethodCallExpression;
            if (call != null)
            {
                if (call.Object != null)
                {
                    var result = FindEnumerableExpression(call.Object);
                    if (result != null) return result;
                }
                foreach (var argument in call.Arguments)
                {
                    var result = FindEnumerableExpression(argument);
                    if (result != null) return result;
                }
            }
            return null;
        }

        private static bool ContainsParameter(Expression expression)
        {
            var visitor = new ParameterFindingVisitor();
            visitor.Visit(expression);
            return visitor.Found;
        }

        private static object Evaluate(Expression expression)
        {
            expression = StripConvert(expression);
            var constant = expression as ConstantExpression;
            if (constant != null) return constant.Value;
            var member = expression as MemberExpression;
            if (member != null && CanEvaluate(member.Expression))
            {
                var instance = member.Expression == null ? null : Evaluate(member.Expression);
                var field = member.Member as FieldInfo;
                if (field != null) return field.GetValue(instance);
                var property = member.Member as PropertyInfo;
                if (property != null) return property.GetValue(instance, null);
            }
            var call = expression as MethodCallExpression;
            if (call != null && CanEvaluate(call.Object) && call.Arguments.All(CanEvaluate))
            {
                var instance = call.Object == null ? null : Evaluate(call.Object);
                return call.Method.Invoke(instance, call.Arguments.Select(Evaluate).ToArray());
            }
            return Expression.Lambda<Func<object>>(Expression.Convert(expression, typeof(object))).Compile()();
        }

        private sealed class ParameterFindingVisitor : ExpressionVisitor
        {
            internal bool Found;
            protected override Expression VisitParameter(ParameterExpression node) { Found = true; return node; }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                // A marker method has no runtime body - it only exists to be translated - so an
                // expression containing one can never be evaluated to a constant.
                if (IsFluentSqlMarker(node.Method))
                {
                    Found = true;
                    return node;
                }
                return base.VisitMethodCall(node);
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                if (node.Member.Name == "Row" && node.Member.DeclaringType != null &&
                    node.Member.DeclaringType.GetTypeInfo().IsGenericType &&
                    node.Member.DeclaringType.GetGenericTypeDefinition() == typeof(TableReference<>))
                {
                    Found = true;
                    return node;
                }
                return base.VisitMember(node);
            }
        }
    }
}
