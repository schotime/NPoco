using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NPoco.Expressions
{
    public class MemberChainHelper
    {
        private static MemberExpression GetMemberExpression(Expression method)
        {
            LambdaExpression lambda = method as LambdaExpression;
            if (lambda == null)
                return null;

            MemberExpression memberExpr = null;

            if (lambda.Body.NodeType == ExpressionType.Convert)
            {
                memberExpr = ((UnaryExpression)lambda.Body).Operand as MemberExpression;
            }
            else if (lambda.Body.NodeType == ExpressionType.MemberAccess)
            {
                memberExpr = lambda.Body as MemberExpression;
            }

            return memberExpr;
        }

        public static IEnumerable<MemberInfo> GetMembers(Expression expression)
        {
            var memberExpression = expression as MemberExpression ?? GetMemberExpression(expression);
            if (memberExpression == null)
            {
                yield break;
            }

            var member = memberExpression.Member;

            foreach (var memberInfo in GetMembers(memberExpression.Expression))
            {
                yield return memberInfo;
            }

            yield return member;
        }
    }
}
