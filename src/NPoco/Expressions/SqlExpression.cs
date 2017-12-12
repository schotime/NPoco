using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using NPoco.Linq;

namespace NPoco.Expressions
{
    public class OrderByMember
    {
        public Type EntityType { get; set; }
        public PocoColumn PocoColumn { get; set; }
        public PocoColumn[] PocoColumns { get; set; }
        public string AscDesc { get; set; }
    }

    public class SelectMember : IEquatable<SelectMember>
    {
        public Type EntityType { get; set; }
        public string SelectSql { get; set; }
        public PocoColumn PocoColumn { get; set; }
        public PocoColumn[] PocoColumns { get; set; }

        public bool Equals(SelectMember other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(EntityType, other.EntityType) && Equals(PocoColumn, other.PocoColumn);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SelectMember) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((EntityType != null ? EntityType.GetHashCode() : 0)*397) ^ (PocoColumn != null ? PocoColumn.GetHashCode() : 0);
            }
        }
    }

    public class GeneralMember
    {
        public Type EntityType { get; set; }
        public PocoColumn PocoColumn { get; set; }
        public PocoColumn[] PocoColumns { get; set; }
    }

    public interface ISqlExpression
    {
        List<OrderByMember> OrderByMembers { get; }
        int? Rows { get; }
        int? Skip { get; }
        string WhereSql { get; }
        object[] Params { get; }
        Type Type { get; }
        List<SelectMember> SelectMembers { get; }
        List<GeneralMember> GeneralMembers { get; }
        string ApplyPaging(string sql, IEnumerable<PocoColumn[]> columns, Dictionary<string, JoinData> joinSqlExpressions);
    }

    public abstract class SqlExpression<T> : ISqlExpression
    {
        private Expression<Func<T, bool>> underlyingExpression;
        private List<string> orderByProperties = new List<string>();
        private List<OrderByMember> orderByMembers = new List<OrderByMember>();
        private List<SelectMember> selectMembers = new List<SelectMember>();
        private List<GeneralMember> generalMembers = new List<GeneralMember>();
        private string selectExpression = string.Empty;
        private string whereExpression;
        private string groupBy = string.Empty;
        private string havingExpression;
        private string orderBy = string.Empty;

        List<OrderByMember> ISqlExpression.OrderByMembers { get { return orderByMembers; } }
        List<SelectMember> ISqlExpression.SelectMembers { get { return selectMembers; } }
        List<GeneralMember> ISqlExpression.GeneralMembers { get { return generalMembers; } }
        string ISqlExpression.WhereSql { get { return whereExpression; } }
        int? ISqlExpression.Rows { get { return Rows; } }
        int? ISqlExpression.Skip { get { return Skip; } }
        Type ISqlExpression.Type { get { return _type; } }
        object[] ISqlExpression.Params { get { return Context.Params; } }

        string ISqlExpression.ApplyPaging(string sql, IEnumerable<PocoColumn[]> columns, Dictionary<string, JoinData> joinSqlExpressions)
        {
            return ApplyPaging(sql, columns, joinSqlExpressions);
        }

        private string sep = string.Empty;
        protected string EscapeChar = "\\";
        private PocoData _pocoData;
        private readonly IDatabase _database;
        private readonly DatabaseType _databaseType;
        private bool PrefixFieldWithTableName { get; set; }
        private Type _type { get; set; }

        public SqlExpression(IDatabase database, PocoData pocoData, bool prefixTableName)
        {
            _type = typeof(T);
            _pocoData = pocoData;
            _database = database;
            _databaseType = database.DatabaseType;
            PrefixFieldWithTableName = prefixTableName;
            paramPrefix = "@";
            Context = new SqlExpressionContext(this);
        }

        public class SqlExpressionContext
        {
            private readonly SqlExpression<T> _expression;

            public SqlExpressionContext(SqlExpression<T> expression)
            {
                _expression = expression;
                UpdateFields = new List<string>();
            }

            public List<string> UpdateFields { get; set; }
            public object[] Params { get { return _expression._params.ToArray(); } }

            public virtual string ToDeleteStatement()
            {
                return _expression.ToDeleteStatement();
            }

            public virtual string ToUpdateStatement(T item)
            {
                return _expression.ToUpdateStatement(item, false);
            }

            public virtual string ToUpdateStatement(T item, bool excludeDefaults)
            {
                return _expression.ToUpdateStatement(item, excludeDefaults);
            }

            public virtual string ToUpdateStatement(T item, bool excludeDefaults, bool allFields)
            {
                if (allFields)
                    _expression.generalMembers = _expression.GetAllMembers().Select(x => new GeneralMember { EntityType = typeof(T), PocoColumn = x }).ToList();

                return _expression.ToUpdateStatement(item, excludeDefaults);
            }

            public string ToWhereStatement()
            {
                return _expression.ToWhereStatement();
            }

            public virtual string ToSelectStatement()
            {
                return ToSelectStatement(true, false);
            }

            public virtual string ToSelectStatement(bool applyPaging, bool distinct)
            {
                return _expression.ToSelectStatement(applyPaging, distinct);
            }
        }

        /// <summary>
        /// Fields to be selected.
        /// </summary>
        /// <param name='fields'>
        /// x=> x.SomeProperty1 or x=> new{ x.SomeProperty1, x.SomeProperty2}
        /// </param>
        /// <typeparam name='TKey'>
        /// objectWithProperties
        /// </typeparam>
        public virtual SqlExpression<T> Select<TKey>(Expression<Func<T, TKey>> fields)
        {
            sep = string.Empty;
            selectMembers.Clear();
            Visit(fields);
            return this;
        }

        public virtual List<SelectMember> SelectProjection<TKey>(Expression<Func<T, TKey>> fields)
        {
            sep = string.Empty;
            selectMembers.Clear();
            _projection = true;
            var exp = PartialEvaluator.Eval(fields, CanBeEvaluatedLocally);
            Visit(exp);
            _projection = false;
            var proj = selectMembers.Union(generalMembers.Select(x=>new SelectMember() { EntityType = x.EntityType, PocoColumn = x.PocoColumn, PocoColumns = x.PocoColumns })).ToList();
            selectMembers.Clear();
            return proj;
        }

        public virtual List<SelectMember> SelectDistinct<TKey>(Expression<Func<T, TKey>> fields)
        {
            return SelectProjection(fields);
        }

        public virtual SqlExpression<T> Where(string sqlFilter, params object[] filterParams)
        {
            if (string.IsNullOrEmpty(sqlFilter))
                return this;

            sqlFilter = ParameterHelper.ProcessParams(sqlFilter, filterParams, _params);

            appendSqlFilter(sqlFilter);

            return this;
        }

        private void appendSqlFilter(string sqlFilter)
        {
            if (string.IsNullOrEmpty(whereExpression))
            {
                whereExpression = "WHERE " + sqlFilter;
            }
            else
            {
                whereExpression += " AND " + sqlFilter;
            }
        }

        public string On<T2>(Expression<Func<T, T2, bool>> predicate)
        {
            sep = " ";
            var onSql = Visit(predicate).ToString();
            return onSql;
        }

        public virtual SqlExpression<T> Where(Expression<Func<T, bool>> predicate)
        {
            if (predicate != null)
            {
                And(predicate);
            }
            else
            {
                underlyingExpression = null;
                whereExpression = string.Empty;
            }

            return this;
        }

        protected virtual SqlExpression<T> And(Expression<Func<T, bool>> predicate)
        {
            if (predicate != null)
            {
                if (underlyingExpression == null)
                    underlyingExpression = predicate;
                else
                    underlyingExpression = underlyingExpression.And(predicate);

                ProcessInternalExpression(predicate);
            }
            return this;
        }

        private void ProcessInternalExpression(Expression<Func<T, bool>> predicate)
        {
            sep = " ";
            var exp = PartialEvaluator.Eval(predicate, CanBeEvaluatedLocally);
            var sqlFilter = Visit(exp).ToString();

            if (!string.IsNullOrEmpty(sqlFilter))
            {
                appendSqlFilter(sqlFilter);
            }
        }

        private bool CanBeEvaluatedLocally(Expression expression)
        {
            // any operation on a query can't be done locally
            ConstantExpression cex = expression as ConstantExpression;
            if (cex != null)
            {
                IQueryable query = cex.Value as IQueryable;
                if (query != null && query.Provider == this)
                    return false;
            }
            MethodCallExpression mc = expression as MethodCallExpression;
            if (mc != null &&
                (mc.Method.DeclaringType == typeof(Enumerable) ||
                 mc.Method.DeclaringType == typeof(Queryable)))
            {
                return false;
            }
            if (expression.NodeType == ExpressionType.Convert &&
                expression.Type == typeof(object))
                return true;
            return expression.NodeType != ExpressionType.Parameter &&
                   expression.NodeType != ExpressionType.Lambda;
        }

        public virtual SqlExpression<T> GroupBy<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            sep = string.Empty;
            groupBy = Visit(keySelector).ToString();
            if (!string.IsNullOrEmpty(groupBy)) groupBy = string.Format("GROUP BY {0}", groupBy);
            return this;
        }

        public virtual SqlExpression<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            sep = string.Empty;
            orderByProperties.Clear();
            orderByMembers.Clear();
            generalMembers.Clear();
            var memberAccess = (MemberAccessString)Visit(keySelector);
            orderByProperties.Add(memberAccess + " ASC");
            orderByMembers.Add(new OrderByMember { AscDesc = "ASC", PocoColumn = memberAccess.PocoColumn, EntityType = memberAccess.Type, PocoColumns = memberAccess.PocoColumns });
            generalMembers.Clear();
            BuildOrderByClauseInternal();
            return this;
        }

        public virtual SqlExpression<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            sep = string.Empty;
            generalMembers.Clear();
            var memberAccess = (MemberAccessString)Visit(keySelector);
            orderByProperties.Add(memberAccess + " ASC");
            orderByMembers.Add(new OrderByMember { AscDesc = "ASC", PocoColumn = memberAccess.PocoColumn, EntityType = memberAccess.Type, PocoColumns = memberAccess.PocoColumns });
            generalMembers.Clear();
            BuildOrderByClauseInternal();
            return this;
        }

        public virtual SqlExpression<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            sep = string.Empty;
            orderByProperties.Clear();
            orderByMembers.Clear();
            generalMembers.Clear();
            var memberAccess = (MemberAccessString)Visit(keySelector);
            orderByProperties.Add(memberAccess + " DESC");
            orderByMembers.Add(new OrderByMember { AscDesc = "DESC", PocoColumn = memberAccess.PocoColumn, EntityType = memberAccess.Type, PocoColumns = memberAccess.PocoColumns });
            generalMembers.Clear();
            BuildOrderByClauseInternal();
            return this;
        }

        public virtual SqlExpression<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            sep = string.Empty;
            generalMembers.Clear();
            var memberAccess = (MemberAccessString)Visit(keySelector);
            orderByProperties.Add(memberAccess + " DESC");
            orderByMembers.Add(new OrderByMember { AscDesc = "DESC", PocoColumn = memberAccess.PocoColumn, EntityType = memberAccess.Type, PocoColumns = memberAccess.PocoColumns });
            generalMembers.Clear();
            BuildOrderByClauseInternal();
            return this;
        }

        private void BuildOrderByClauseInternal()
        {
            if (orderByMembers.Count > 0)
            {
                orderBy = "ORDER BY " + string.Join(", ", orderByMembers.Select(x => (PrefixFieldWithTableName ? _databaseType.EscapeSqlIdentifier(x.PocoColumns.Last().MemberInfoKey) : _databaseType.EscapeSqlIdentifier(x.PocoColumns.Last().MemberInfoKey)) + " " + x.AscDesc).ToArray());
            }
            else
            {
                orderBy = null;
            }
        }


        /// <summary>
        /// Set the specified offset and rows for SQL Limit clause.
        /// </summary>
        /// <param name='skip'>
        /// Offset of the first row to return. The offset of the initial row is 0
        /// </param>
        /// <param name='rows'>
        /// Number of rows returned by a SELECT statement
        /// </param>
        public virtual SqlExpression<T> Limit(int skip, int rows)
        {
            Rows = rows;
            Skip = skip;
            return this;
        }

        /// <summary>
        /// Set the specified rows for Sql Limit clause.
        /// </summary>
        /// <param name='rows'>
        /// Number of rows returned by a SELECT statement
        /// </param>
        public virtual SqlExpression<T> Limit(int rows)
        {
            Rows = rows;
            Skip = 0;
            return this;
        }

        /// <summary>
        /// Fields to be updated.
        /// </summary>
        /// <param name='fields'>
        /// x=> x.SomeProperty1 or x=> new{ x.SomeProperty1, x.SomeProperty2}
        /// </param>
        /// <typeparam name='TKey'>
        /// objectWithProperties
        /// </typeparam>
        public virtual SqlExpression<T> Update<TKey>(Expression<Func<T, TKey>> fields)
        {
            sep = string.Empty;
            generalMembers.Clear();
            Visit(fields);
            Context.UpdateFields = new List<string>(generalMembers.Select(x => x.PocoColumn.MemberInfoData.Name));
            generalMembers.Clear();
            return this;
        }

        protected virtual string ToDeleteStatement()
        {
            return string.Format("DELETE {0} FROM {1} {2}",
                (PrefixFieldWithTableName ? _databaseType.EscapeTableName(_pocoData.TableInfo.AutoAlias) : string.Empty),
                _databaseType.EscapeTableName(_pocoData.TableInfo.TableName) + (PrefixFieldWithTableName ? " " + _databaseType.EscapeTableName(_pocoData.TableInfo.AutoAlias) : string.Empty),
                WhereExpression);
        }

        protected virtual string ToUpdateStatement(T item)
        {
            return ToUpdateStatement(item, false);
        }

        protected virtual string ToUpdateStatement(T item, bool excludeDefaults)
        {
            var setFields = new StringBuilder();
            var primaryKeys = _pocoData.TableInfo.PrimaryKey.Split(',');

            foreach (var fieldDef in _pocoData.Columns)
            {
                if (_pocoData.TableInfo.AutoIncrement && primaryKeys.Contains(fieldDef.Value.ColumnName, StringComparer.OrdinalIgnoreCase))
                    continue;

                if (Context.UpdateFields.Count > 0 && !Context.UpdateFields.Contains(fieldDef.Value.MemberInfoData.Name)) continue; // added
                object value = fieldDef.Value.GetColumnValue(_pocoData, item, (pocoColumn, val) => ProcessMapperExtensions.ProcessMapper(_database, pocoColumn, val));
                if (_database.Mappers != null)
                {
                    value = _database.Mappers.FindAndExecute(x => x.GetToDbConverter(fieldDef.Value.ColumnType, fieldDef.Value.MemberInfoData.MemberInfo), value);
                }

                if (excludeDefaults && (value == null || value.Equals(MappingHelper.GetDefault(value.GetType())))) continue; //GetDefaultValue?

                if (setFields.Length > 0)
                    setFields.Append(", ");

                setFields.AppendFormat("{0} = {1}", (PrefixFieldWithTableName ? _databaseType.EscapeTableName(_pocoData.TableInfo.AutoAlias) + "." : string.Empty) + _databaseType.EscapeSqlIdentifier(fieldDef.Value.ColumnName), CreateParam(value));
            }

            if (PrefixFieldWithTableName)
                return string.Format("UPDATE {0} SET {2} FROM {1} {3}", _databaseType.EscapeTableName(_pocoData.TableInfo.AutoAlias), _databaseType.EscapeTableName(_pocoData.TableInfo.TableName) + " " + _databaseType.EscapeTableName(_pocoData.TableInfo.AutoAlias), setFields, WhereExpression);
            else
                return string.Format("UPDATE {0} SET {1} {2}", _databaseType.EscapeTableName(_pocoData.TableInfo.TableName), setFields, WhereExpression);
        }

        protected string ToWhereStatement()
        {
            return WhereExpression;
        }

        protected virtual string ToSelectStatement(bool applyPaging, bool isDistinct)
        {
            var sql = new StringBuilder();

            sql.Append(GetSelectExpression(isDistinct));
            sql.Append(string.IsNullOrEmpty(WhereExpression) ?
                       "" :
                       " \n" + WhereExpression);
            sql.Append(string.IsNullOrEmpty(GroupByExpression) ?
                       "" :
                       " \n" + GroupByExpression);
            sql.Append(string.IsNullOrEmpty(HavingExpression) ?
                       "" :
                       " \n" + HavingExpression);
            sql.Append(string.IsNullOrEmpty(OrderByExpression) ?
                       "" :
                       " \n" + OrderByExpression);

            return applyPaging ? ApplyPaging(sql.ToString(), ModelDef.QueryColumns.Select(x=> new[] { x.Value }), new Dictionary<string, JoinData>()) : sql.ToString();
        }

        private string GetSelectExpression(bool distinct)
        {
            var selectMembersFromOrderBys = orderByMembers
                .Select(x => new SelectMember() { PocoColumn = x.PocoColumn, EntityType = x.EntityType, PocoColumns = new[] { x.PocoColumn }})
                .Where(x => !selectMembers.Any(y => y.EntityType == x.EntityType && y.PocoColumn.MemberInfoData.Name == x.PocoColumn.MemberInfoData.Name));

            var morecols = selectMembers.Concat(selectMembersFromOrderBys);
            var cols = selectMembers.Count == 0 ? null : morecols.ToList();
            var selectsql = BuildSelectExpression(cols, distinct);
            return selectsql;
        }

        private string WhereExpression
        {
            get
            {
                return whereExpression;
            }
            set
            {
                whereExpression = value;
            }
        }

        private string GroupByExpression
        {
            get
            {
                return groupBy;
            }
            set
            {
                groupBy = value;
            }
        }

        private string HavingExpression
        {
            get
            {
                return havingExpression;
            }
            set
            {
                havingExpression = value;
            }
        }


        private string OrderByExpression
        {
            get
            {
                return orderBy;
            }
            set
            {
                orderBy = value;
            }
        }

        protected virtual string LimitExpression
        {
            get
            {
                if (!Skip.HasValue) return "";
                string rows;
                if (Rows.HasValue)
                {
                    rows = string.Format(",{0}", Rows.Value);
                }
                else
                {
                    rows = string.Empty;
                }
                return string.Format("LIMIT {0}{1}", Skip.Value, rows);
            }
        }

        private int? Rows { get; set; }
        private int? Skip { get; set; }

        protected internal PocoData ModelDef
        {
            get
            {
                return _pocoData;
            }
            set
            {
                _pocoData = value;
            }
        }

        protected internal virtual object Visit(Expression exp)
        {

            if (exp == null) return string.Empty;
            switch (exp.NodeType)
            {
                case ExpressionType.Lambda:
                    return VisitLambda(exp as LambdaExpression);
                case ExpressionType.MemberAccess:
                    return VisitMemberAccess(exp as MemberExpression);
                case ExpressionType.Constant:
                    return VisitConstant(exp as ConstantExpression);
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.Divide:
                case ExpressionType.Modulo:
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.Coalesce:
                case ExpressionType.ArrayIndex:
                case ExpressionType.RightShift:
                case ExpressionType.LeftShift:
                case ExpressionType.ExclusiveOr:
                    //return "(" + VisitBinary(exp as BinaryExpression) + ")";
                    return VisitBinary(exp as BinaryExpression);
                case ExpressionType.Conditional:
                    return VisitConditional(exp as ConditionalExpression);
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                case ExpressionType.Not:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.ArrayLength:
                case ExpressionType.Quote:
                case ExpressionType.TypeAs:
                    return VisitUnary(exp as UnaryExpression);
                case ExpressionType.Parameter:
                    return VisitParameter(exp as ParameterExpression);
                case ExpressionType.Call:
                    return VisitMethodCall(exp as MethodCallExpression);
                case ExpressionType.New:
                    return VisitNew(exp as NewExpression);
                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                    return VisitNewArray(exp as NewArrayExpression);
                case ExpressionType.MemberInit:
                    return this.VisitMemberInit((MemberInitExpression)exp);
                default:
                    return exp.ToString();
            }
        }
        protected virtual Expression VisitMemberInit(MemberInitExpression init)
        {
            NewExpression n = init.NewExpression;
            IEnumerable<MemberBinding> bindings = this.VisitBindingList(init.Bindings);
            if (n != init.NewExpression || bindings != init.Bindings)
            {
                return Expression.MemberInit(n, bindings);
            }
            return init;
        }

        protected virtual IEnumerable<MemberBinding> VisitBindingList(ReadOnlyCollection<MemberBinding> original)
        {
            for (int i = 0, n = original.Count; i < n; i++)
            {
                this.VisitBinding(original[i]);
            }
            return original;
        }

        protected virtual object VisitBinding(MemberBinding binding)
        {
            switch (binding.BindingType)
            {
                case MemberBindingType.Assignment:
                    return this.VisitMemberAssignment((MemberAssignment)binding);
                case MemberBindingType.MemberBinding:
                    return this.VisitMemberMemberBinding((MemberMemberBinding)binding);
                //case MemberBindingType.ListBinding:
                //    return this.VisitMemberListBinding((MemberListBinding)binding);
                default:
                    throw new Exception(string.Format("Unhandled binding type '{0}'", binding.BindingType));
            }
        }

        protected virtual object VisitMemberMemberBinding(MemberMemberBinding binding)
        {
            return VisitBindingList(binding.Bindings);
        }

        protected virtual object VisitMemberAssignment(MemberAssignment assignment)
        {
            return this.Visit(assignment.Expression);
        }

        protected virtual object VisitLambda(LambdaExpression lambda)
        {
            if (lambda.Body.NodeType == ExpressionType.MemberAccess && sep == " ")
            {
                MemberExpression m = lambda.Body as MemberExpression;

                if (m.Expression != null)
                {
                    if (IsNullableMember(m))
                    {
                        string r = VisitMemberAccess(m.Expression as MemberExpression).ToString();
                        return string.Format("{0} is not null", r);
                    }
                    else
                    {
                        string r = VisitMemberAccess(m).ToString();
                        return string.Format("{0}={1}", r, GetQuotedTrueValue());
                    }
                }
            }
            else if (lambda.Body.NodeType == ExpressionType.Constant)
            {
                var result = Visit(lambda.Body);
                if (result is bool)
                {
                    return ((bool) result) ? "1=1" : "1<>1";
                }
            }
            return Visit(lambda.Body);
        }

        private static bool IsNullableMember(MemberExpression m)
        {
            var member = m.Expression as MemberExpression;
            return member != null
                && member.Type.GetTypeInfo().IsGenericType && member.Type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        protected virtual object VisitBinary(BinaryExpression b)
        {
            // Fix VB and CompareString
            b = FixExpressionForVb(b);

            object left, right;
            bool switchLeftRight = false;
            var operand = BindOperant(b.NodeType);   //sep= " " ??

            if (b.NodeType == ExpressionType.AndAlso || b.NodeType == ExpressionType.OrElse)
            {
                var m = b.Left as MemberExpression;
                if (m != null && m.Expression != null
                    && m.Expression.NodeType == ExpressionType.Parameter)
                    left = new PartialSqlString(string.Format("{0} = {1}", VisitMemberAccess(m), GetQuotedTrueValue()));
                else
                    left = Visit(b.Left);

                if (left is NullableMemberAccess)
                {
                    left = new PartialSqlString("(" + left + " is not null)");
                }

                m = b.Right as MemberExpression;
                if (m != null && m.Expression != null
                    && m.Expression.NodeType == ExpressionType.Parameter)
                    right = new PartialSqlString(string.Format("{0} = {1}", VisitMemberAccess(m), GetQuotedTrueValue()));
                else
                    right = Visit(b.Right);

                if (right is NullableMemberAccess)
                {
                    right = new PartialSqlString("(" + right + " is not null)");
                }

                if (!(left is PartialSqlString) && !(right is PartialSqlString))
                {
                    var result = Expression.Lambda(b).Compile().DynamicInvoke();
                    return new PartialSqlString(CreateParam(result));
                }

                if (!(left is PartialSqlString))
                    left = ((bool)left) ? GetTrueExpression() : GetFalseExpression();
                if (!(right is PartialSqlString))
                    right = ((bool)right) ? GetTrueExpression() : GetFalseExpression();
            }
            else
            {
                left = Visit(b.Left);
                right = Visit(b.Right);

                var isLeftMemberAccessString = left.CanBeCastTo<MemberAccessString>(out var leftMemberAccessString);
                var isRightMemberAccessString = right.CanBeCastTo<MemberAccessString>(out var rightMemberAccessString);

                // Enums
                if (left.CanBeCastTo<EnumMemberAccess>(out var leftEnumMemberAccess) && !(right is PartialSqlString))
                {
                    var pc = leftEnumMemberAccess.PocoColumn;
                    if (pc.ColumnType == typeof(string))
                        right = CreateParam(Enum.Parse(GetMemberInfoTypeForEnum(pc), right.ToString()).ToString());
                    else if (Int64.TryParse(right.ToString(), out long numvericVal))
                        right = CreateParam(Enum.ToObject(GetMemberInfoTypeForEnum(pc), numvericVal));
                    else
                        right = CreateParam(right);
                }
                else if (right.CanBeCastTo<EnumMemberAccess>(out var rightEnumMemberAccess) && !(left is PartialSqlString))
                {
                    var pc = rightEnumMemberAccess.PocoColumn;
                    if (pc.ColumnType == typeof(string))
                        left = CreateParam(Enum.Parse(GetMemberInfoTypeForEnum(pc), left.ToString()).ToString());
                    else if (Int64.TryParse(left.ToString(), out long numvericVal))
                        left = CreateParam(Enum.ToObject(GetMemberInfoTypeForEnum(pc), numvericVal));
                    else
                        left = CreateParam(left);
                }
                // Nullable Members
                else if (left is NullableMemberAccess && !(right is PartialSqlString))
                {
                    operand = ((bool)right) ? "is not" : "is";
                    right = new PartialSqlString("null");
                }
                else if (right is NullableMemberAccess && !(left is PartialSqlString))
                {
                    operand = ((bool)left) ? "is not" : "is";
                    left = new PartialSqlString("null");
                    switchLeftRight = true;
                }
                // Chars
                else if (isLeftMemberAccessString && right is int
                    && new [] { typeof(char), typeof(char?) }.Contains(leftMemberAccessString.PocoColumn.MemberInfoData.MemberType))
                {
                    right = CreateParam(Convert.ToChar(right));
                }
                else if (isRightMemberAccessString && left is int
                         && new[] { typeof(char), typeof(char?) }.Contains(rightMemberAccessString.PocoColumn.MemberInfoData.MemberType))
                {
                    left = CreateParam(Convert.ToChar(left));
                }
                // AnsiString
                else if (isLeftMemberAccessString && right is string && leftMemberAccessString.PocoColumn.ColumnType == typeof (AnsiString))
                {
                    right = CreateParam(new AnsiString((string)right));
                }
                else if (isRightMemberAccessString && left is string && rightMemberAccessString.PocoColumn.ColumnType == typeof(AnsiString))
                {
                    left = CreateParam(new AnsiString((string)left));
                }
                // ValueObject
                else if (isLeftMemberAccessString && leftMemberAccessString.PocoColumn.ValueObjectColumn)
                {
                    right = CreateParam(leftMemberAccessString.PocoColumn.GetValueObjectValue(right));
                }
                else if (isRightMemberAccessString && rightMemberAccessString.PocoColumn.ValueObjectColumn)
                {
                    left = CreateParam(rightMemberAccessString.PocoColumn.GetValueObjectValue(left));
                }
                else if (!(left is PartialSqlString) && !(right is PartialSqlString))
                {
                    var result = Expression.Lambda(b).Compile().DynamicInvoke();
                    return result;
                }
                else if (!(left is PartialSqlString))
                    left = CreateParam(left);
                else if (!(right is PartialSqlString))
                    right = CreateParam(right);

            }

            if (operand == "=" && right.ToString().Equals("null", StringComparison.OrdinalIgnoreCase)) { operand = "is"; }
            else if (operand == "=" && left.ToString().Equals("null", StringComparison.OrdinalIgnoreCase)) { operand = "is"; switchLeftRight = true; }
            else if (operand == "<>" && right.ToString().Equals("null", StringComparison.OrdinalIgnoreCase)) { operand = "is not"; }
            else if (operand == "<>" && left.ToString().Equals("null", StringComparison.OrdinalIgnoreCase)) { operand = "is not"; switchLeftRight = true; }

            // Switch left and right for situtations like is null
            if (switchLeftRight)
            {
                var saveleft = left;
                left = right;
                right = saveleft;
            }

            switch (operand)
            {
                case "MOD":
                case "COALESCE":
                    return new PartialSqlString(string.Format("{0}({1},{2})", operand, left, right));
                default:
                    return new PartialSqlString("(" + left + sep + operand + sep + right + ")");
            }
        }

        private static BinaryExpression FixExpressionForVb(BinaryExpression b)
        {
            if (b.Left is MethodCallExpression)
            {
                var method = (MethodCallExpression) b.Left;
                if (method.Method.Name == "CompareString"
                    && method.Method.DeclaringType.FullName == "Microsoft.VisualBasic.CompilerServices.Operators")
                {
                    var left = method.Arguments[0];
                    var right = method.Arguments[1];

                    return b.NodeType == ExpressionType.Equal ? Expression.Equal(left, right) : Expression.NotEqual(left, right);
                }
            }
            return b;
        }

        private static Type GetMemberInfoTypeForEnum(PocoColumn pc)
        {
            if (pc.MemberInfoData.MemberType.GetTypeInfo().IsEnum)
                return pc.MemberInfoData.MemberType;

            return Nullable.GetUnderlyingType(pc.MemberInfoData.MemberType);
        }

        protected virtual object VisitMemberAccess(MemberExpression m)
        {
            bool isNull = false;

            if (IsNullableMember(m))
            {
                if (m.Member.Name == "HasValue")
                {
                    isNull = true;
                }
                m = m.Expression as MemberExpression;
            }

            if (m.Expression != null
                && (m.Expression.NodeType == ExpressionType.Parameter
                    || m.Expression.NodeType == ExpressionType.Convert
                    || m.Expression.NodeType == ExpressionType.MemberAccess))
            {
                var propertyInfos = MemberChainHelper.GetMembers(m).ToArray();
                var type = GetCorrectType(m);

                var pocoMembers = ModelDef.GetAllMembers()
                    .Where(x => x.MemberInfoChain.Select(y => y.Name).SequenceEqual(propertyInfos.Select(y => y.Name)))
                    .ToArray();

                var pocoMember = pocoMembers.LastOrDefault();
                if (pocoMember == null)
                {
                    throw new Exception(
                        string.Format("Did you forget to include the property eg. Include(x => x.{0})",
                        string.Join(".", propertyInfos.Select(y => y.Name).Take(propertyInfos.Length - 1).ToArray())));
                }

                if (_projection &&
                    (pocoMember.ReferenceType == ReferenceType.Foreign
                    || pocoMember.ReferenceType == ReferenceType.OneToOne)
                    || pocoMember.PocoColumn == null)
                {
                    foreach (var member in pocoMember.PocoMemberChildren.Where(x => x.PocoColumn != null))
                    {
                        generalMembers.Add(new GeneralMember()
                        {
                            EntityType = pocoMember.MemberInfoData.MemberType,
                            PocoColumn = member.PocoColumn,
                            PocoColumns = new [] { member.PocoColumn }
                        });
                    }

                    return new PartialSqlString("");
                }

                var pocoColumn = pocoMember.PocoColumn;
                var pocoColumns = pocoMembers.Select(x => x.PocoColumn).ToArray();

                var columnName = (PrefixFieldWithTableName
                                          ? _databaseType.EscapeTableName(pocoColumn.TableInfo.AutoAlias) + "."
                                          : "")
                                     + _databaseType.EscapeSqlIdentifier(pocoColumn.ColumnName);

                generalMembers.Add(new GeneralMember()
                {
                    EntityType = type,
                    PocoColumn = pocoColumn,
                    PocoColumns = pocoColumns
                });

                if (isNull)
                    return new NullableMemberAccess(pocoColumn, pocoColumns, columnName, type);

                if (Database.IsEnum(pocoColumn.MemberInfoData))
                    return new EnumMemberAccess(pocoColumn, pocoColumns, columnName, type);

                return new MemberAccessString(pocoColumn, pocoColumns, columnName, type);
            }

            var memberExp = Expression.Convert(m, typeof(object));
            var lambda = Expression.Lambda<Func<object>>(memberExp);
            var getter = lambda.Compile();
            return getter();
        }

        private Type GetCorrectType(MemberExpression m)
        {
            var type = m.Member.DeclaringType;
            if (m.Expression.NodeType == ExpressionType.MemberAccess)
            {
                type = ((PropertyInfo)((MemberExpression)m.Expression).Member).PropertyType;
            }
            else if (m.Expression.NodeType == ExpressionType.Parameter)
            {
                type = m.Expression.Type;
            }
            return type;
        }

        protected virtual object VisitNew(NewExpression nex)
        {
            var member = Expression.Convert(nex, typeof(object));
            var lambda = Expression.Lambda<Func<object>>(member);
            try
            {
                var getter = lambda.Compile();
                return getter();
            }
            catch (System.InvalidOperationException)
            {
                List<PartialSqlString> exprs = VisitExpressionList(nex.Arguments).OfType<PartialSqlString>().ToList();
                StringBuilder r = new StringBuilder();
                for (int i = 0; i < exprs.Count; i++)
                {
                    if (exprs[i] is MemberAccessString)
                    {
                        selectMembers.Add(new SelectMember()
                        {
                            EntityType = ((MemberAccessString)exprs[i]).Type,
                            PocoColumn = ((MemberAccessString)exprs[i]).PocoColumn,
                            PocoColumns = ((MemberAccessString)exprs[i]).PocoColumns,
                        });
                    }
                }
                return r.ToString();
            }

        }

        protected virtual object VisitParameter(ParameterExpression p)
        {
            return p.Name;
        }

        List<object> _params = new List<object>();
        
        string paramPrefix;
        private bool _projection;
        public SqlExpressionContext Context { get; private set; }

        protected virtual object VisitConstant(ConstantExpression c)
        {
            if (c.Value == null)
                return new PartialSqlString("null");

            return c.Value;
        }

        protected virtual object VisitConditional(ConditionalExpression conditional)
        {
            sep = " ";
            var test = Visit(conditional.Test);
            var trueSql = Visit(conditional.IfTrue);
            var falseSql = Visit(conditional.IfFalse);

            return new PartialSqlString(string.Format("(case when {0} then {1} else {2} end)", test, trueSql, falseSql));
        }

        protected string CreateParam(object value)
        {
            string paramPlaceholder = paramPrefix + _params.Count;
            _params.Add(value);
            return paramPlaceholder;
        }

        protected virtual object VisitUnary(UnaryExpression u)
        {
            switch (u.NodeType)
            {
                case ExpressionType.Not:
                    var o = Visit(u.Operand);

                    if (o as PartialSqlString == null)
                        return !((bool)o);

                    if (o as MemberAccessString != null)
                    {
                        if (o as NullableMemberAccess != null)
                            o = o + " is not null";
                        else
                            o = o + " = " + GetQuotedTrueValue();
                    }

                    return new PartialSqlString("NOT (" + o + ")");
                case ExpressionType.Convert:
                    if (u.Method != null)
                        return Expression.Lambda(u).Compile().DynamicInvoke();
                    break;
            }

            return Visit(u.Operand);

        }

        private bool IsColumnAccess(MethodCallExpression m)
        {
            if (m.Object != null && m.Object as MethodCallExpression != null)
                return IsColumnAccess(m.Object as MethodCallExpression);

            var exp = m.Object as MemberExpression;
            return exp != null
                && exp.Expression != null
                && ((exp.Expression.Type == typeof(T) && exp.Expression.NodeType == ExpressionType.Parameter
                    || exp.Expression.NodeType == ExpressionType.MemberAccess));
        }

        protected virtual object VisitMethodCall(MethodCallExpression m)
        {
            if (m.Method.DeclaringType == typeof(S))
                return VisitSqlMethodCall(m);

            if (IsStaticArrayMethod(m))
                return VisitStaticArrayMethodCall(m);

            if (IsEnumerableMethod(m))
                return VisitEnumerableMethodCall(m);

            if (IsColumnAccess(m))
                return VisitColumnAccessMethod(m);

            if (_projection && VisitInnerMethodCall(m))
                return null;

            return Expression.Lambda(m).Compile().DynamicInvoke();
        }

        private bool VisitInnerMethodCall(MethodCallExpression m)
        {
            bool found = false;
            if (m.Arguments.Any(args => ProcessMethodSearchRecursively(args, ref found)))
            {
                return true;
            }
            return found;
        }

        private bool ProcessMethodSearchRecursively(Expression args, ref bool found)
        {
            if (args.NodeType == ExpressionType.Parameter && args.Type == typeof (T))
            {
                selectMembers.AddRange(_pocoData.QueryColumns.Select(x => new SelectMember { PocoColumn = x.Value, EntityType = _pocoData.Type, PocoColumns = new[] { x.Value } }));
                return true;
            }

            IEnumerable<Expression> nestedExpressions = null;
            var nested1 = args as MethodCallExpression;
            if (nested1 != null)
            {
                nestedExpressions = nested1.Arguments;
            }
            else
            {
                var nested2 = args as NewArrayExpression;
                if (nested2 != null) nestedExpressions = nested2.Expressions;
            }

            if (nestedExpressions != null)
            {
                foreach (var nestedExpression in nestedExpressions)
                {
                    if (ProcessMethodSearchRecursively(nestedExpression, ref found))
                        return true;
                }
            }

            var result = Visit(args) as MemberAccessString;
            found = found || result != null;

            return false;
        }

        private bool IsStaticArrayMethod(MethodCallExpression m)
        {
            if (m.Object == null && m.Method.Name == "Contains")
            {
                return m.Arguments.Count == 2;
            }

            return false;
        }

        private bool IsEnumerableMethod(MethodCallExpression m)
        {
            if (m.Object != null
                && m.Object.Type.IsOrHasGenericInterfaceTypeOf(typeof(IEnumerable<>))
                && m.Object.Type != typeof(string)
                && m.Method.Name == "Contains")
            {
                return m.Arguments.Count == 1;
            }

            return false;
        }

        protected virtual object VisitEnumerableMethodCall(MethodCallExpression m)
        {
            switch (m.Method.Name)
            {
                case "Contains":
                    List<Object> args = this.VisitExpressionList(m.Arguments);
                    return new PartialSqlString(BuildInStatement(m.Object, args[0]));

                default:
                    throw new NotSupportedException();
            }
        }

        protected virtual object VisitStaticArrayMethodCall(MethodCallExpression m)
        {
            switch (m.Method.Name)
            {
                case "Contains":
                    List<Object> args = this.VisitExpressionList(m.Arguments);
                    Expression memberExpr = m.Arguments[0];
                    if (memberExpr.NodeType == ExpressionType.MemberAccess)
                        memberExpr = (m.Arguments[0] as MemberExpression);

                    return new PartialSqlString(BuildInStatement(memberExpr, args[1]));

                default:
                    throw new NotSupportedException();
            }
        }

        private StringBuilder FlattenList(List<object> inArgs, object partialSqlString)
        {
            var sIn = new StringBuilder();
            foreach (object e in inArgs)
            {
                if (!typeof(ICollection).IsAssignableFrom(e.GetType()))
                {
                    var v = FormatParameters(partialSqlString, e);
                    sIn.AppendFormat("{0}{1}", sIn.Length > 0 ? "," : "", CreateParam(v));
                }
                else
                {
                    foreach (object el in (ICollection)e)
                    {
                        var v = FormatParameters(partialSqlString, el);
                        sIn.AppendFormat("{0}{1}", sIn.Length > 0 ? "," : "", CreateParam(v));
                    }
                }
            }

            return sIn;
        }

        private static object FormatParameters(object partialSqlString, object e)
        {
            if (partialSqlString is EnumMemberAccess && ((EnumMemberAccess)partialSqlString).PocoColumn.ColumnType == typeof(string))
            {
                e = e.ToString();
            }
            return e;
        }

        protected virtual List<Object> VisitExpressionList(ReadOnlyCollection<Expression> original)
        {
            List<Object> list = new List<Object>();
            for (int i = 0, n = original.Count; i < n; i++)
            {
                if (original[i].NodeType == ExpressionType.NewArrayInit ||
                 original[i].NodeType == ExpressionType.NewArrayBounds)
                {

                    list.AddRange(VisitNewArrayFromExpressionList(original[i] as NewArrayExpression));
                }
                else
                    list.Add(Visit(original[i]));

            }
            return list;
        }

        protected virtual List<Object> VisitConstantList(ReadOnlyCollection<Expression> original)
        {
            List<Object> list = new List<Object>();
            for (int i = 0, n = original.Count; i < n; i++)
            {
                list.Add(original[i].GetConstantValue<object>());
            }
            return list;
        }

        protected virtual object VisitNewArray(NewArrayExpression na)
        {

            List<Object> exprs = VisitExpressionList(na.Expressions);
            StringBuilder r = new StringBuilder();
            foreach (Object e in exprs)
            {
                r.Append(r.Length > 0 ? "," + e : e);
            }

            return r.ToString();
        }

        protected virtual List<Object> VisitNewArrayFromExpressionList(NewArrayExpression na)
        {

            List<Object> exprs = VisitExpressionList(na.Expressions);
            return exprs;
        }


        protected virtual string BindOperant(ExpressionType e)
        {
            switch (e)
            {
                case ExpressionType.Equal:
                    return "=";
                case ExpressionType.NotEqual:
                    return "<>";
                case ExpressionType.GreaterThan:
                    return ">";
                case ExpressionType.GreaterThanOrEqual:
                    return ">=";
                case ExpressionType.LessThan:
                    return "<";
                case ExpressionType.LessThanOrEqual:
                    return "<=";
                case ExpressionType.AndAlso:
                    return "AND";
                case ExpressionType.OrElse:
                    return "OR";
                case ExpressionType.Add:
                    return "+";
                case ExpressionType.Subtract:
                    return "-";
                case ExpressionType.Multiply:
                    return "*";
                case ExpressionType.Divide:
                    return "/";
                case ExpressionType.Modulo:
                    return "MOD";
                case ExpressionType.Coalesce:
                    return "COALESCE";
                case ExpressionType.And:
                    return "&";
                case ExpressionType.Or:
                    return "|";
                case ExpressionType.ExclusiveOr:
                    return "^";
                case ExpressionType.Not:
                    return "~";
                default:
                    return e.ToString();
            }
        }

        protected virtual string GetQuotedColumnName(string memberName)
        {
            var fd = _pocoData.Columns.Values.FirstOrDefault(x => x.MemberInfoData.Name == memberName);
            string fn = fd != null ? fd.ColumnName : memberName;
            return _databaseType.EscapeSqlIdentifier(fn);
        }

        protected string RemoveQuoteFromAlias(string exp)
        {

            if ((exp.StartsWith("\"") || exp.StartsWith("`") || exp.StartsWith("'"))
                && (exp.EndsWith("\"") || exp.EndsWith("`") || exp.EndsWith("'")))
            {
                exp = exp.Remove(0, 1);
                exp = exp.Remove(exp.Length - 1, 1);
            }
            return exp;
        }

        protected object GetTrueExpression()
        {
            return new PartialSqlString(string.Format("({0}={1})", GetQuotedTrueValue(), GetQuotedTrueValue()));
        }

        protected object GetFalseExpression()
        {
            return new PartialSqlString(string.Format("({0}={1})", GetQuotedTrueValue(), GetQuotedFalseValue()));
        }

        protected object GetQuotedTrueValue()
        {
            return CreateParam(true);
        }

        protected object GetQuotedFalseValue()
        {
            return CreateParam(false);
        }

        private string BuildSelectExpression(List<SelectMember> fields, bool distinct)
        {
            var cols = fields ?? _pocoData.QueryColumns.Select(x => new SelectMember{ PocoColumn = x.Value, EntityType = _pocoData.Type, PocoColumns = new[] { x.Value }});
            return string.Format("SELECT {0}{1} \nFROM {2}",
                (distinct ? "DISTINCT " : ""),
                    string.Join(", ", cols.Select(x =>
                    {
                        if (x.SelectSql == null)
                            return (PrefixFieldWithTableName
                                ? _databaseType.EscapeTableName(_pocoData.TableInfo.AutoAlias) + "." + _databaseType.EscapeSqlIdentifier(x.PocoColumn.ColumnName) + " as " + _databaseType.EscapeSqlIdentifier(x.PocoColumns.Last().MemberInfoKey)
                                : _databaseType.EscapeSqlIdentifier(x.PocoColumn.ColumnName));
                        return x.SelectSql;
                    }).ToArray()),
                    _databaseType.EscapeTableName(_pocoData.TableInfo.TableName) + (PrefixFieldWithTableName ? " " + _databaseType.EscapeTableName(_pocoData.TableInfo.AutoAlias) : string.Empty));
        }

        internal List<PocoColumn> GetAllMembers()
        {
            return _pocoData.Columns.Values.ToList();
        }

        protected virtual string ApplyPaging(string sql, IEnumerable<PocoColumn[]> columns, Dictionary<string, JoinData> joinSqlExpressions)
        {
            if (!Rows.HasValue || Rows == 0)
                return sql;

            string sqlPage;
            var parms = _params.Select(x => x).ToArray();

            // Split the SQL
            PagingHelper.SQLParts parts;
            if (!PagingHelper.SplitSQL(sql, out parts)) throw new Exception("Unable to parse SQL statement for paged query");

            if (columns != null && columns.Any() && _databaseType.UseColumnAliases())
            {
                parts.sqlColumns = string.Join(", ", columns.Select(x => _databaseType.EscapeSqlIdentifier(x.Last().MemberInfoKey)).ToArray());
            }

            sqlPage = _databaseType.BuildPageQuery(Skip ?? 0, Rows ?? 0, parts, ref parms);

            _params.Clear();
            _params.AddRange(parms);

            return sqlPage;
        }

        private string BuildInStatement(Expression m, object quotedColName)
        {
            var member = Expression.Convert(m, typeof(object));
            var lambda = Expression.Lambda<Func<object>>(member);
            var getter = lambda.Compile();

            if (quotedColName == null)
                quotedColName = Visit(m);

            var inArgs = ((IEnumerable) getter()).Cast<object>().ToList();
            if (inArgs.Count == 0)
            {
                return "1 = 0";
            }

            var sIn = FlattenList(inArgs, quotedColName);
            var statement = string.Format("{0} {1} ({2})", quotedColName, "IN", sIn);
            return statement;
        }

        protected virtual object VisitSqlMethodCall(MethodCallExpression m)
        {
            List<Object> args = this.VisitExpressionList(m.Arguments);
            object quotedColName = args[0];
            args.RemoveAt(0);

            string statement;

            switch (m.Method.Name)
            {
                case "In":
                    statement = BuildInStatement(m.Arguments[1], quotedColName);
                    break;
                case "Desc":
                    statement = string.Format("{0} DESC", quotedColName);
                    break;
                case "As":
                    statement = string.Format("{0} As {1}", quotedColName,
                        _databaseType.EscapeSqlIdentifier(RemoveQuoteFromAlias(args[0].ToString())));
                    break;
                case "Sum":
                case "Count":
                case "Min":
                case "Max":
                case "Avg":
                    statement = string.Format("{0}({1}{2})",
                                         m.Method.Name.ToUpper(),
                                         quotedColName,
                                         args.Count == 1 ? string.Format(",{0}", args[0]) : "");
                    break;
                default:
                    throw new NotSupportedException();
            }

            return new PartialSqlString(statement);
        }

        protected virtual object VisitColumnAccessMethod(MethodCallExpression m)
        {
            var expression = (PartialSqlString)Visit(m.Object);

            if (_projection && expression is MemberAccessString)
                return expression;

            string statement;
            List<Object> args = this.VisitExpressionList(m.Arguments);

            switch (m.Method.Name)
            {
                case "ToUpper":
                    statement = string.Format("upper({0})", expression);
                    break;
                case "ToLower":
                    statement = string.Format("lower({0})", expression);
                    break;
                case "StartsWith":
                    statement = CreateLikeStatement(expression, CreateParam(EscapeParam(args[0]) + "%"));
                    break;
                case "EndsWith":
                    statement = CreateLikeStatement(expression, CreateParam("%" + EscapeParam(args[0])));
                    break;
                case "Contains":
                    statement = CreateLikeStatement(expression, CreateParam("%" + EscapeParam(args[0]) + "%"));
                    break;
                case "Substring":
                    var startIndex = Int32.Parse(args[0].ToString()) + 1;
                    var length = (args.Count > 1) ? Int32.Parse(args[1].ToString()) : -1;
                    statement = SubstringStatement(expression, startIndex, length);
                    break;
                case "Equals":
                    statement = string.Format("({0} = {1})", expression, CreateParam(args[0]));
                    break;
                case "ToString":
                    statement = string.Empty;
                    break;
                default:
                    throw new NotSupportedException();
            }

            return new PartialSqlString(statement);
        }

        protected virtual string CreateLikeStatement(PartialSqlString expression, string param)
        {
            return string.Format("upper({0}) like {1} escape '{2}'", expression, param, EscapeChar);
        }

        protected virtual string EscapeParam(object par)
        {
            var param = par.ToString().ToUpper();
            param = param
                .Replace(EscapeChar, EscapeChar + EscapeChar)
                .Replace("_", EscapeChar + "_");
            return param;
        }

        // Easy to override
        protected virtual string SubstringStatement(PartialSqlString columnName, int startIndex, int length)
        {
            if (length >= 0)
                return string.Format("substring({0},{1},{2})", columnName, CreateParam(startIndex), CreateParam(length));
            else
                return string.Format("substring({0},{1},8000)", columnName, CreateParam(startIndex));
        }
    }

    public class PartialSqlString
    {
        public PartialSqlString(string text)
        {
            Text = text;
        }
        public string Text { get; set; }
        public override string ToString()
        {
            return Text;
        }
    }

    public class MemberAccessString : PartialSqlString
    {
        public MemberAccessString(PocoColumn pocoColumn, PocoColumn[] pocoColumns, string text, Type type)
            : base(text)
        {
            PocoColumn = pocoColumn;
            PocoColumns = pocoColumns;
            Type = type;
        }

        public PocoColumn PocoColumn { get; private set; }
        public PocoColumn[] PocoColumns { get; private set; }
        public Type Type { get; set; }
    }

    public class NullableMemberAccess : MemberAccessString
    {
        public NullableMemberAccess(PocoColumn pocoColumn, PocoColumn[] pocoColumns, string text, Type type)
            : base(pocoColumn, pocoColumns, text, type)
        {
        }
    }

    public class EnumMemberAccess : MemberAccessString
    {
        public EnumMemberAccess(PocoColumn pocoColumn, PocoColumn[] pocoColumns, string text, Type type)
            : base(pocoColumn, pocoColumns, text, type)
        {
        }
    }

    public static class LinqExtensions
    {
        /// <summary>
        /// Gets the constant value.
        /// </summary>
        /// <param retval="exp">The exp.</param>
        /// <returns>The get constant value.</returns>
        public static T GetConstantValue<T>(this Expression exp)
        {
            T result = default(T);
            if (exp is ConstantExpression)
            {
                var c = (ConstantExpression)exp;

                result = (T)c.Value;
            }

            return result;
        }
    }

}