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
        public string AscDesc { get; set; }
    }

    public class SelectMember : IEquatable<SelectMember>
    {
        public Type EntityType { get; set; }
        public string SelectSql { get; set; }
        public PocoColumn PocoColumn { get; set; }

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
        string ApplyPaging(string sql, IEnumerable<PocoColumn> columns);
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

        string ISqlExpression.ApplyPaging(string sql, IEnumerable<PocoColumn> columns)
        {
            return ApplyPaging(sql, columns);
        }

        private string sep = string.Empty;
        private PocoData modelDef;
        private readonly IDatabase _database;
        private readonly DatabaseType _databaseType;
        private bool PrefixFieldWithTableName { get; set; }
        private bool WhereStatementWithoutWhereString { get; set; }
        private Type _type { get; set; }

        public SqlExpression(IDatabase database, bool prefixTableName)
        {
            _type = typeof(T);
            modelDef = database.PocoDataFactory.ForType(typeof(T));
            _database = database;
            _databaseType = database.DatabaseType;
            PrefixFieldWithTableName = prefixTableName;
            WhereStatementWithoutWhereString = false;
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
        /// Clear select expression. All properties will be selected.
        /// </summary>
        //public virtual SqlExpression<T> Select()
        //{
        //    return Select(string.Empty);
        //}

        /// <summary>
        /// set the specified selectExpression.
        /// </summary>
        /// <param name='selectExpression'>
        /// raw Select expression: "Select SomeField1, SomeField2 from SomeTable"
        /// </param>
        //public virtual SqlExpression<T> Select(string selectExpression)
        //{
        //    if (!string.IsNullOrEmpty(selectExpression))
        //    {
        //        this.selectExpression = selectExpression;
        //    }
        //    return this;
        //}

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
            var proj = new List<SelectMember>(selectMembers.Union(generalMembers.Select(x=>new SelectMember() { EntityType = x.EntityType, PocoColumn = x.PocoColumn })));
            selectMembers.Clear();
            return proj;
        }

        public virtual List<SelectMember> SelectDistinct<TKey>(Expression<Func<T, TKey>> fields)
        {
            return SelectProjection(fields);
        }

        //public virtual SqlExpression<T> Where()
        //{
        //    if (underlyingExpression != null) underlyingExpression = null; //Where() clears the expression
        //    return Where(string.Empty);
        //}

        public virtual SqlExpression<T> Where(string sqlFilter, params object[] filterParams)
        {
            whereExpression = !string.IsNullOrEmpty(sqlFilter) ? sqlFilter : string.Empty;
            foreach (var filterParam in filterParams)
            {
                CreateParam(filterParam);
            }
            if (!string.IsNullOrEmpty(whereExpression)) whereExpression = (WhereStatementWithoutWhereString ? "" : "WHERE ") + whereExpression;
            return this;
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

                ProcessInternalExpression();
            }
            return this;
        }

        //public virtual SqlExpression<T> Or(Expression<Func<T, bool>> predicate)
        //{
        //    if (predicate != null)
        //    {
        //        if (underlyingExpression == null)
        //            underlyingExpression = predicate;
        //        else
        //            underlyingExpression = underlyingExpression.Or(predicate);

        //        ProcessInternalExpression();
        //    }
        //    return this;
        //}

        private void ProcessInternalExpression()
        {
            sep = " ";
            var exp = PartialEvaluator.Eval(underlyingExpression, CanBeEvaluatedLocally);
            whereExpression = Visit(exp).ToString();
            if (!string.IsNullOrEmpty(whereExpression)) whereExpression = (WhereStatementWithoutWhereString ? "" : "WHERE ") + whereExpression;
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

        //public virtual SqlExpression<T> GroupBy()
        //{
        //    return GroupBy(string.Empty);
        //}

        //public virtual SqlExpression<T> GroupBy(string groupBy)
        //{
        //    this.groupBy = groupBy;
        //    return this;
        //}

        public virtual SqlExpression<T> GroupBy<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            sep = string.Empty;
            groupBy = Visit(keySelector).ToString();
            if (!string.IsNullOrEmpty(groupBy)) groupBy = string.Format("GROUP BY {0}", groupBy);
            return this;
        }

        //public virtual SqlExpression<T> Having()
        //{
        //    return Having(string.Empty);
        //}

        //public virtual SqlExpression<T> Having(string sqlFilter, params object[] filterParams)
        //{
        //    havingExpression = !string.IsNullOrEmpty(sqlFilter) ? sqlFilter : string.Empty;
        //    foreach (var filterParam in filterParams)
        //    {
        //        CreateParam(filterParam);
        //    }
        //    if (!string.IsNullOrEmpty(havingExpression)) havingExpression = "HAVING " + havingExpression;
        //    return this;
        //}

        //public virtual SqlExpression<T> Having(Expression<Func<T, bool>> predicate)
        //{

        //    if (predicate != null)
        //    {
        //        sep = " ";
        //        havingExpression = Visit(predicate).ToString();
        //        if (!string.IsNullOrEmpty(havingExpression)) havingExpression = "HAVING " + havingExpression;
        //    }
        //    else
        //        havingExpression = string.Empty;

        //    return this;
        //}



        //public virtual SqlExpression<T> OrderBy()
        //{
        //    return OrderBy(string.Empty);
        //}

        //public virtual SqlExpression<T> OrderBy(string orderBy)
        //{
        //    orderByProperties.Clear();
        //    this.orderBy = orderBy;
        //    return this;
        //}

        public virtual SqlExpression<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            sep = string.Empty;
            orderByProperties.Clear();
            orderByMembers.Clear();
            generalMembers.Clear();
            var memberAccess = (MemberAccessString)Visit(keySelector);
            orderByProperties.Add(memberAccess + " ASC");
            orderByMembers.Add(new OrderByMember { AscDesc = "ASC", PocoColumn = memberAccess.PocoColumn, EntityType = memberAccess.Type});
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
            orderByMembers.Add(new OrderByMember { AscDesc = "ASC", PocoColumn = memberAccess.PocoColumn, EntityType = memberAccess.Type });
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
            orderByMembers.Add(new OrderByMember { AscDesc = "DESC", PocoColumn = memberAccess.PocoColumn, EntityType = memberAccess.Type });
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
            orderByMembers.Add(new OrderByMember { AscDesc = "DESC", PocoColumn = memberAccess.PocoColumn, EntityType = memberAccess.Type });
            generalMembers.Clear();
            BuildOrderByClauseInternal();
            return this;
        }

        private void BuildOrderByClauseInternal()
        {
            if (orderByMembers.Count > 0)
            {
                orderBy = "ORDER BY " + string.Join(", ", orderByMembers.Select(x => (PrefixFieldWithTableName ? _databaseType.EscapeSqlIdentifier(x.PocoColumn.AutoAlias) : _databaseType.EscapeSqlIdentifier(x.PocoColumn.ColumnName)) + " " + x.AscDesc).ToArray());
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
        /// Clear Sql Limit clause
        /// </summary>
        //public virtual SqlExpression<T> Limit()
        //{
        //    Skip = null;
        //    Rows = null;
        //    return this;
        //}


        /// <summary>
        /// Fields to be updated.
        /// </summary>
        /// <param name='updatefields'>
        /// IList<string> containing Names of properties to be updated
        /// </param>
        //public virtual SqlExpression<T> Update(IList<string> updateFields)
        //{
        //    this.updateFields = updateFields;
        //    return this;
        //}

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
            Context.UpdateFields = new List<string>(generalMembers.Select(x => x.PocoColumn.MemberInfo.Name));
            generalMembers.Clear();
            return this;
        }

        /// <summary>
        /// Clear UpdateFields list ( all fields will be updated)
        /// </summary>
        //public virtual SqlExpression<T> Update()
        //{
        //    this.updateFields = new List<string>();
        //    return this;
        //}

        /// <summary>
        /// Fields to be inserted.
        /// </summary>
        /// <param name='fields'>
        /// x=> x.SomeProperty1 or x=> new{ x.SomeProperty1, x.SomeProperty2}
        /// </param>
        /// <typeparam name='TKey'>
        /// objectWithProperties
        /// </typeparam>
        //public virtual SqlExpression<T> Insert<TKey>(Expression<Func<T, TKey>> fields)
        //{
        //    sep = string.Empty;
        //    Context.InsertFields = Visit(fields).ToString().Split(',').ToList();
        //    return this;
        //}

        /// <summary>
        /// fields to be inserted.
        /// </summary>
        /// <param name='insertFields'>
        /// IList&lt;string&gt; containing Names of properties to be inserted
        /// </param>
        //public virtual SqlExpression<T> Insert(IList<string> insertFields)
        //{
        //    this.insertFields = insertFields;
        //    return this;
        //}

        /// <summary>
        /// Clear InsertFields list ( all fields will be inserted)
        /// </summary>
        //public virtual SqlExpression<T> Insert()
        //{
        //    this.insertFields = new List<string>();
        //    return this;
        //}

        protected virtual string ToDeleteStatement()
        {
            return string.Format("DELETE {0} FROM {1} {2}",
                (PrefixFieldWithTableName ? _databaseType.EscapeTableName(modelDef.TableInfo.AutoAlias) : string.Empty),
                _databaseType.EscapeTableName(modelDef.TableInfo.TableName) + (PrefixFieldWithTableName ? " " + _databaseType.EscapeTableName(modelDef.TableInfo.AutoAlias) : string.Empty),
                WhereExpression);
        }

        protected virtual string ToUpdateStatement(T item)
        {
            return ToUpdateStatement(item, false);
        }

        protected virtual string ToUpdateStatement(T item, bool excludeDefaults)
        {
            var setFields = new StringBuilder();

            foreach (var fieldDef in modelDef.Columns)
            {
                if (Context.UpdateFields.Count > 0 && !Context.UpdateFields.Contains(fieldDef.Value.MemberInfo.Name)) continue; // added
                object value = fieldDef.Value.GetValue(item);
                if (_database.Mapper != null)
                {
                    var converter = _database.Mapper.GetToDbConverter(fieldDef.Value.ColumnType, fieldDef.Value.MemberInfo);
                    if (converter != null)
                        value = converter(value);
                }

                if (excludeDefaults && (value == null || value.Equals(MappingFactory.GetDefault(value.GetType())))) continue; //GetDefaultValue?

                if (setFields.Length > 0)
                    setFields.Append(", ");

                setFields.AppendFormat("{0} = {1}", (PrefixFieldWithTableName ? _databaseType.EscapeTableName(modelDef.TableInfo.AutoAlias) + "." : string.Empty) + _databaseType.EscapeSqlIdentifier(fieldDef.Value.ColumnName), CreateParam(value));
            }

            if (PrefixFieldWithTableName)
                return string.Format("UPDATE {0} SET {2} FROM {1} {3}", _databaseType.EscapeTableName(modelDef.TableInfo.AutoAlias), _databaseType.EscapeTableName(modelDef.TableInfo.TableName) + " " + _databaseType.EscapeTableName(modelDef.TableInfo.AutoAlias), setFields, WhereExpression);
            else
                return string.Format("UPDATE {0} SET {1} {2}", _databaseType.EscapeTableName(modelDef.TableInfo.TableName), setFields, WhereExpression);
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

            return applyPaging ? ApplyPaging(sql.ToString(), ModelDef.QueryColumns.Select(x=>x.Value)) : sql.ToString();
        }

        //public virtual string ToCountStatement()
        //{
        //    return OrmLiteConfig.DialectProvider.ToCountStatement(modelDef.ModelType, WhereExpression, null);
        //}

        private string GetSelectExpression(bool distinct)
        {
            var selectMembersFromOrderBys = orderByMembers
                .Select(x => new SelectMember() { PocoColumn = x.PocoColumn, EntityType = x.EntityType })
                .Where(x => !selectMembers.Any(y => y.EntityType == x.EntityType && y.PocoColumn.MemberInfo.Name == x.PocoColumn.MemberInfo.Name));

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
                return modelDef;
            }
            set
            {
                modelDef = value;
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
                && (m.Member.Name == "HasValue")
                && member.Type.IsGenericType && member.Type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        protected virtual object VisitBinary(BinaryExpression b)
        {
            object left, right;
            var operand = BindOperant(b.NodeType);   //sep= " " ??
            if (operand == "AND" || operand == "OR")
            {
                var m = b.Left as MemberExpression;
                if (m != null && m.Expression != null
                    && m.Expression.NodeType == ExpressionType.Parameter)
                    left = new PartialSqlString(string.Format("{0} = {1}", VisitMemberAccess(m), GetQuotedTrueValue()));
                else
                    left = Visit(b.Left);

                m = b.Right as MemberExpression;
                if (m != null && m.Expression != null
                    && m.Expression.NodeType == ExpressionType.Parameter)
                    right = new PartialSqlString(string.Format("{0} = {1}", VisitMemberAccess(m), GetQuotedTrueValue()));
                else
                    right = Visit(b.Right);

                if (left as PartialSqlString == null && right as PartialSqlString == null)
                {
                    var result = Expression.Lambda(b).Compile().DynamicInvoke();
                    return new PartialSqlString(CreateParam(result));
                }

                if (left as PartialSqlString == null)
                    left = ((bool)left) ? GetTrueExpression() : GetFalseExpression();
                if (right as PartialSqlString == null)
                    right = ((bool)right) ? GetTrueExpression() : GetFalseExpression();
            }
            else
            {
                left = Visit(b.Left);
                right = Visit(b.Right);

                if (left as EnumMemberAccess != null && right as PartialSqlString == null)
                {
                    var pc = ((EnumMemberAccess)left).PocoColumn;

                    long numvericVal;
                    if (pc.ColumnType == typeof(string))
                        right = CreateParam(Enum.Parse(GetMemberInfoTypeForEnum(pc), right.ToString()).ToString());
                    else if (Int64.TryParse(right.ToString(), out numvericVal))
                        right = CreateParam(Enum.ToObject(GetMemberInfoTypeForEnum(pc), numvericVal));
                    else
                        right = CreateParam(right);
                }
                else if (left as NullableMemberAccess != null && right as PartialSqlString == null)
                {
                    operand = ((bool)right) ? "is not" : "is";
                    right = new PartialSqlString("null");
                }
                else if (right as EnumMemberAccess != null && left as PartialSqlString == null)
                {
                    var pc = ((EnumMemberAccess)right).PocoColumn;

                    //enum value was returned by Visit(b.Left)
                    long numvericVal;
                    if (pc.ColumnType == typeof(string))
                        left = CreateParam(Enum.Parse(GetMemberInfoTypeForEnum(pc), left.ToString()).ToString());
                    else if (Int64.TryParse(left.ToString(), out numvericVal))
                        left = CreateParam(Enum.ToObject(GetMemberInfoTypeForEnum(pc), numvericVal));
                    else
                        left = CreateParam(left);
                }
                else if (left as MemberAccessString != null
                    && right is int
                    && new [] { typeof(char), typeof(char?) }.Contains(((MemberAccessString)left).PocoColumn.MemberInfo.GetMemberInfoType()))
                {
                    right = CreateParam(Convert.ToChar(right));
                }
                else if (left as MemberAccessString != null 
                    && right is string
                    && ((MemberAccessString) left).PocoColumn.ColumnType == typeof (AnsiString))
                {
                    right = CreateParam(new AnsiString((string)right));
                }
                else if (left as PartialSqlString == null && right as PartialSqlString == null)
                {
                    var result = Expression.Lambda(b).Compile().DynamicInvoke();
                    return result;
                }
                else if (left as PartialSqlString == null)
                    left = CreateParam(left);
                else if (right as PartialSqlString == null)
                    right = CreateParam(right);

            }

            if (operand == "=" && right.ToString().Equals("null", StringComparison.InvariantCultureIgnoreCase)) operand = "is";
            else if (operand == "<>" && right.ToString().Equals("null", StringComparison.InvariantCultureIgnoreCase)) operand = "is not";

            switch (operand)
            {
                case "MOD":
                case "COALESCE":
                    return new PartialSqlString(string.Format("{0}({1},{2})", operand, left, right));
                default:
                    return new PartialSqlString("(" + left + sep + operand + sep + right + ")");
            }
        }

        private static Type GetMemberInfoTypeForEnum(PocoColumn pc)
        {
            if (pc.MemberInfo.GetMemberInfoType().IsEnum)
                return pc.MemberInfo.GetMemberInfoType();

            return Nullable.GetUnderlyingType(pc.MemberInfo.GetMemberInfoType());
        }

        protected virtual object VisitMemberAccess(MemberExpression m)
        {
            bool isNull = false;

            if (IsNullableMember(m))
            {
                m = m.Expression as MemberExpression;
                isNull = true;
            }

            if (m.Expression != null
                && (m.Expression.NodeType == ExpressionType.Parameter
                    || m.Expression.NodeType == ExpressionType.Convert
                    || m.Expression.NodeType == ExpressionType.MemberAccess))
            {
                var propertyInfo = (PropertyInfo)m.Member;

                var type = GetCorrectType(m);
                var localModelDef = _database.PocoDataFactory.ForType(type);
                var pocoColumn = localModelDef.Columns.Values.Single(x => x.MemberInfo.Name == m.Member.Name);

                var columnName = (PrefixFieldWithTableName ? _databaseType.EscapeTableName(localModelDef.TableInfo.AutoAlias) + "." : "")
                                    + _databaseType.EscapeSqlIdentifier(pocoColumn.ColumnName);

                generalMembers.Add(new GeneralMember() { EntityType = type, PocoColumn = pocoColumn });

                if (isNull)
                    return new NullableMemberAccess(pocoColumn, columnName, type);

                if (Database.IsEnum(propertyInfo))
                    return new EnumMemberAccess(pocoColumn, columnName, type);

                return new MemberAccessString(pocoColumn, columnName, type);
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
                    if (_projection && exprs[i] is MemberAccessString)
                    {
                        selectMembers.Add(new SelectMember()
                        {
                            EntityType = ((MemberAccessString)exprs[i]).Type,
                            PocoColumn = ((MemberAccessString)exprs[i]).PocoColumn
                        });
                        continue;
                    }

                    r.AppendFormat("{0}{1}", r.Length > 0 ? "," : "", exprs[i]);
                    if (nex.Members[i] != null)
                    {
                        var col = modelDef.Columns.SingleOrDefault(x => x.Value.MemberInfo.Name == nex.Members[i].Name);
                        if (col.Value != null)
                        {
                            var sel = new SelectMember()
                            {
                                EntityType = modelDef.type,
                                SelectSql = exprs[i].ToString(),
                                PocoColumn = col.Value
                            };
                            var memberName = _databaseType.EscapeSqlIdentifier(col.Value.ColumnName);
                            if (memberName != exprs[i].ToString())
                            {
                                var al = string.Format(" AS {0}", _databaseType.EscapeSqlIdentifier(col.Value.AutoAlias));
                                r.AppendFormat(al);
                                sel.SelectSql += al;
                            }
                            selectMembers.Add(sel);
                        }
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
        int _paramCounter = 0;
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
            string paramPlaceholder = paramPrefix + _paramCounter++;
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
                selectMembers.AddRange(modelDef.QueryColumns.Select(x => new SelectMember {PocoColumn = x.Value, EntityType = modelDef.type}));
                return true;
            }

            var nested = args as MethodCallExpression;
            if (nested != null)
            {
                foreach (var nestedArgs in nested.Arguments)
                {
                    if (ProcessMethodSearchRecursively(nestedArgs, ref found))
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

        private StringBuilder FlattenList(IEnumerable inArgs, object partialSqlString)
        {
            StringBuilder sIn = new StringBuilder();
            foreach (Object e in inArgs)
            {
                if (!typeof(ICollection).IsAssignableFrom(e.GetType()))
                {
                    var v = FormatParameters(partialSqlString, e);
                    sIn.AppendFormat("{0}{1}", sIn.Length > 0 ? "," : "", CreateParam(v));
                }
                else
                {
                    var listArgs = e as ICollection;
                    foreach (Object el in listArgs)
                    {
                        var v = FormatParameters(partialSqlString, el);
                        sIn.AppendFormat("{0}{1}", sIn.Length > 0 ? "," : "", CreateParam(v));
                    }
                }
            }

            if (sIn.Length == 0)
            {
                sIn.AppendFormat("select 1 /*poco_dual*/ where 1 = 0");
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
                default:
                    return e.ToString();
            }
        }

        protected virtual string GetQuotedColumnName(string memberName)
        {
            var fd = modelDef.Columns.Values.FirstOrDefault(x => x.MemberInfo.Name == memberName);
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
            var cols = fields ?? modelDef.QueryColumns.Select(x => new SelectMember{ PocoColumn = x.Value, EntityType = modelDef.type });
            return string.Format("SELECT {0}{1} \nFROM {2}",
                (distinct ? "DISTINCT " : ""),
                    string.Join(", ", cols.Select(x =>
                    {
                        if (x.SelectSql == null)
                            return (PrefixFieldWithTableName
                                ? _databaseType.EscapeTableName(modelDef.TableInfo.AutoAlias) + "." + _databaseType.EscapeSqlIdentifier(x.PocoColumn.ColumnName) + " as " + _databaseType.EscapeSqlIdentifier(x.PocoColumn.AutoAlias)
                                : _databaseType.EscapeSqlIdentifier(x.PocoColumn.ColumnName));
                        return x.SelectSql;
                    }).ToArray()),
                    _databaseType.EscapeTableName(modelDef.TableInfo.TableName) + (PrefixFieldWithTableName ? " " + _databaseType.EscapeTableName(modelDef.TableInfo.AutoAlias) : string.Empty));
        }

        internal List<PocoColumn> GetAllMembers()
        {
            return modelDef.Columns.Values.ToList();
        }

        protected virtual string ApplyPaging(string sql, IEnumerable<PocoColumn> columns)
        {
            if (!Rows.HasValue || Rows == 0)
                return sql;

            string sqlPage;
            var parms = _params.Select(x => x).ToArray();

            // Split the SQL
            PagingHelper.SQLParts parts;
            if (!PagingHelper.SplitSQL(sql, out parts)) throw new Exception("Unable to parse SQL statement for paged query");

            if (columns != null && columns.Any() && _databaseType.UseColumnAliases()) 
                parts.sqlColumns = string.Join(", ", columns.Select(x => _databaseType.EscapeSqlIdentifier(x.AutoAlias)).ToArray());

            sqlPage = _databaseType.BuildPageQuery(Skip ?? 0, Rows ?? 0, parts, ref parms);

            _params.Clear();
            _params.AddRange(parms);

            return sqlPage;
        }

        private string BuildInStatement(Expression m, object quotedColName)
        {
            string statement;
            var member = Expression.Convert(m, typeof(object));
            var lambda = Expression.Lambda<Func<object>>(member);
            var getter = lambda.Compile();

            if (quotedColName == null)
                quotedColName = Visit(m);

            var inArgs = getter() as IEnumerable;

            var sIn = FlattenList(inArgs, quotedColName);

            statement = string.Format("{0} {1} ({2})", quotedColName, "IN", sIn);
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
                    statement = string.Format("upper({0}) like {1}", expression, CreateParam(args[0].ToString().ToUpper() + "%"));
                    break;
                case "EndsWith":
                    statement = string.Format("upper({0}) like {1}", expression, CreateParam("%" + args[0].ToString().ToUpper()));
                    break;
                case "Contains":
                    statement = string.Format("upper({0}) like {1}", expression, CreateParam("%" + args[0].ToString().ToUpper() + "%"));
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
        public MemberAccessString(PocoColumn pocoColumn, string text, Type type) 
            : base(text)
        {
            PocoColumn = pocoColumn;
            Type = type;
        }

        public PocoColumn PocoColumn { get; private set; }
        public Type Type { get; set; }
    }

    public class NullableMemberAccess : MemberAccessString
    {
        public NullableMemberAccess(PocoColumn pocoColumn, string text, Type type) : base(pocoColumn, text, type)
        {
        }
    }

    public class EnumMemberAccess : MemberAccessString
    {
        public EnumMemberAccess(PocoColumn pocoColumn, string text, Type type) : base(pocoColumn, text, type)
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