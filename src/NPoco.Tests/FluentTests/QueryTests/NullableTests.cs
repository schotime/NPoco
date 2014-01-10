using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NPoco.Expressions;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.FluentTests.QueryTests
{
    public class NullableTests : BaseDBFuentTest
    {
        [Test]
        public void NullableExpressionNotHasValue_ReturnsIsNotIsNotNull()
        {
            var sqlExpression = new DefaultSqlExpression<NullableProberty>(Database);
            sqlExpression.Where(x => !x.Age.HasValue);
            var whereStatement = sqlExpression.Context.ToWhereStatement();
            Assert.AreEqual("WHERE NOT ([Age] is not null)", whereStatement);
        }

        [Test]
        public void NullableExpressionHasValue_ReturnsIsNotNull()
        {
            var sqlExpression = new DefaultSqlExpression<NullableProberty>(Database);
            sqlExpression.Where(x => x.Age.HasValue);
            var whereStatement = sqlExpression.Context.ToWhereStatement();
            Assert.AreEqual("WHERE [Age] is not null", whereStatement);
        }

        [Test]
        public void NullableExpressionHasValueEqualTrue_ReturnsIsNotIsNotNull()
        {
            var sqlExpression = new DefaultSqlExpression<NullableProberty>(Database);
            sqlExpression.Where(x => x.Age.HasValue == true);
            var whereStatement = sqlExpression.Context.ToWhereStatement();
            Assert.AreEqual("WHERE ([Age] is not null)", whereStatement);
        }

        [Test]
        public void NullableExpressionHasValueEqualFalse_ReturnsIsNull()
        {
            var sqlExpression = new DefaultSqlExpression<NullableProberty>(Database);
            sqlExpression.Where(x => x.Age.HasValue == false);
            var whereStatement = sqlExpression.Context.ToWhereStatement();
            Assert.AreEqual("WHERE ([Age] is null)", whereStatement);
        }

        [Test]
        public void NullableExpressionEqualsNull_ReturnsIsNull()
        {
            var sqlExpression = new DefaultSqlExpression<NullableProberty>(Database);
            sqlExpression.Where(x => x.Age == null);
            var whereStatement = sqlExpression.Context.ToWhereStatement();
            Assert.AreEqual("WHERE ([Age] is null)", whereStatement);
        }

        [Test]
        public void NullableExpressionNotEqualsNull_ReturnsIsNotNull()
        {
            var sqlExpression = new DefaultSqlExpression<NullableProberty>(Database);
            sqlExpression.Where(x => x.Age != null);
            var whereStatement = sqlExpression.Context.ToWhereStatement();
            Assert.AreEqual("WHERE ([Age] is not null)", whereStatement);
        }
        
        public class NullableProberty
        {
            public bool? Age { get; set; }
        }
    }
}
