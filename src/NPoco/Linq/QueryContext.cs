using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NPoco.Expressions;

namespace NPoco.Linq
{
    public class QueryContext<T>
    {
        private readonly Database _database;
        private readonly PocoData _pocoData;
        private readonly Dictionary<string, JoinData> _joinExpressions;

        public QueryContext(Database database, PocoData pocoData, Dictionary<string, JoinData> joinExpressions)
        {
            _database = database;
            _pocoData = pocoData;
            _joinExpressions = joinExpressions;
        }

        public DatabaseType DatabaseType
        {
            get { return _database.DatabaseType; }
        }
        
        public string GetAliasFor(Expression<Func<T, object>> propertyExpression)
        {
            var member = MemberChainHelper.GetMembers(propertyExpression).LastOrDefault();
            if (member == null)
                return _pocoData.TableInfo.AutoAlias;

            var pocoMember = _joinExpressions.Values.SingleOrDefault(x => x.PocoMember.MemberInfoData.MemberInfo.Name == member.Name);
            if (pocoMember == null)
                throw new Exception("Tried to get alias for table that has not been included");

            return pocoMember.PocoMemberJoin.PocoColumn.TableInfo.AutoAlias;
        }

        public PocoData PocoData =>_database.PocoDataFactory.ForType(typeof(T));

        public PocoData GetPocoDataFor<TModel>()
        {
            return _database.PocoDataFactory.ForType(typeof (TModel));
        }
    }
}