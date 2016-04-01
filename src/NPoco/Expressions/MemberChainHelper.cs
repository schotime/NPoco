using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace NPoco.Expressions
{
    public class MemberChainHelper
    {
        private static MemberExpression GetMemberExpression(Expression method)
        {
            MemberExpression memberExpr = null;
            LambdaExpression lambda = method as LambdaExpression;
            if (lambda == null)
            {
                var call = method as MethodCallExpression;
                if (call != null)
                {
                    if (call.Method.Name == "get_Item")
                        return call.Object as MemberExpression;

                    if ((call.Method.Name == "First" || call.Method.Name == "FirstOrDefault") && call.Arguments.Count >= 1)
                        return call.Arguments.First() as MemberExpression;
                }
                return null;
            }
            
            if (lambda.Body.NodeType == ExpressionType.Convert)
            {
                memberExpr = ((UnaryExpression) lambda.Body).Operand as MemberExpression;
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