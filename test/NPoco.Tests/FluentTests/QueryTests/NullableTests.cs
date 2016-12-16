using NPoco.Expressions;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.FluentTests.QueryTests
{
    public class NullableTests : BaseDBFuentTest
    {
        private string escapedAgeIdentifier;

        [SetUp]
        public void GetEscapedAgeIdentifier()
        {
            escapedAgeIdentifier = TestDatabase.DbType.EscapeSqlIdentifier("Age");
        }

        [Test]
        public void NullableExpressionNotHasValue_ReturnsIsNotIsNotNull()
        {
            var sqlExpression = new DefaultSqlExpression<NullableProperty>(Database);
            sqlExpression.Where(x => !x.Age.HasValue);
            var whereStatement = sqlExpression.Context.ToWhereStatement();
            string expected = string.Format("WHERE NOT ({0} is not null)", escapedAgeIdentifier);
            Assert.AreEqual(expected, whereStatement);
        }

        [Test]
        public void NullableExpressionHasValueWithAnotherExpression_ReturnsIsNotNull()
        {
            var sqlExpression = new DefaultSqlExpression<NullableProperty>(Database);
            sqlExpression.Where(x => x.Age.HasValue && x.Age > 2);
            var whereStatement = sqlExpression.Context.ToWhereStatement();
            string expected = string.Format("WHERE (({0} is not null) AND ([Age] > @0))", escapedAgeIdentifier);
            Assert.AreEqual(expected, whereStatement);
        }

        [Test]
        public void NullableExpressionNotHasValueWithAnotherExpression_ReturnsNotIsNotNull()
        {
            var sqlExpression = new DefaultSqlExpression<NullableProperty>(Database);
            sqlExpression.Where(x => !x.Age.HasValue && x.Age > 2);
            var whereStatement = sqlExpression.Context.ToWhereStatement();
            string expected = string.Format("WHERE (NOT ({0} is not null) AND ([Age] > @0))", escapedAgeIdentifier);
            Assert.AreEqual(expected, whereStatement);
        }

        [Test]
        public void ExpressionWithNullableExpressionNotHasValue_ReturnsNotIsNotNull()
        {
            var sqlExpression = new DefaultSqlExpression<NullableProperty>(Database);
            sqlExpression.Where(x => x.Age > 2 && x.Age.HasValue);
            var whereStatement = sqlExpression.Context.ToWhereStatement();
            string expected = string.Format("WHERE (([Age] > @0) AND ({0} is not null))", escapedAgeIdentifier);
            Assert.AreEqual(expected, whereStatement);
        }

        [Test]
        public void NullableExpressionHasValue_ReturnsIsNotNull()
        {
            var sqlExpression = new DefaultSqlExpression<NullableProperty>(Database);
            sqlExpression.Where(x => x.Age.HasValue);
            var whereStatement = sqlExpression.Context.ToWhereStatement();
            string expected = string.Format("WHERE {0} is not null", escapedAgeIdentifier);
            Assert.AreEqual(expected, whereStatement);
        }

        [Test]
        public void NullableExpressionHasValueEqualTrue_ReturnsIsNotIsNotNull()
        {
            var sqlExpression = new DefaultSqlExpression<NullableProperty>(Database);
            sqlExpression.Where(x => x.Age.HasValue == true);
            var whereStatement = sqlExpression.Context.ToWhereStatement();
            string expected = string.Format("WHERE ({0} is not null)", escapedAgeIdentifier);
            Assert.AreEqual(expected, whereStatement);
        }

        [Test]
        public void NullableExpressionHasValueEqualFalse_ReturnsIsNull()
        {
            var sqlExpression = new DefaultSqlExpression<NullableProperty>(Database);
            sqlExpression.Where(x => x.Age.HasValue == false);
            var whereStatement = sqlExpression.Context.ToWhereStatement();
            string expected = string.Format("WHERE ({0} is null)", escapedAgeIdentifier);
            Assert.AreEqual(expected, whereStatement);
        }

        [Test]
        public void NullableExpressionEqualsNull_ReturnsIsNull()
        {
            var sqlExpression = new DefaultSqlExpression<NullableProperty>(Database);
            sqlExpression.Where(x => x.Age == null);
            var whereStatement = sqlExpression.Context.ToWhereStatement();
            string expected = string.Format("WHERE ({0} is null)", escapedAgeIdentifier);
            Assert.AreEqual(expected, whereStatement);
        }

        [Test]
        public void NullableExpressionNotEqualsNull_ReturnsIsNotNull()
        {
            var sqlExpression = new DefaultSqlExpression<NullableProperty>(Database);
            sqlExpression.Where(x => x.Age != null);
            var whereStatement = sqlExpression.Context.ToWhereStatement();
            string expected = string.Format("WHERE ({0} is not null)", escapedAgeIdentifier);
            Assert.AreEqual(expected, whereStatement);
        }
        
        public class NullableProperty
        {
            public int? Age { get; set; }
        }
    }
}
