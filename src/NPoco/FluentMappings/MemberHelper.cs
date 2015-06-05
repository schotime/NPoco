using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NPoco.Expressions;

namespace NPoco.FluentMappings
{
    public static class MemberHelper<T>
    {
        public static MemberInfo[] GetMembers<TValue>(Expression<Func<T, TValue>> selector)
        {
            //Expression body = selector;
            //if (body is LambdaExpression)
            //{
            //    body = ((LambdaExpression)body).Body;
            //}
            //if (body is UnaryExpression)
            //{
            //    body = ((UnaryExpression)body).Operand;
            //}
            //switch (body.NodeType)
            //{
            //    case ExpressionType.MemberAccess:
            //        return ((MemberExpression)body).Member;
            //    default:
            //        throw new InvalidOperationException();
            //}

            return MemberChainHelper.GetMembers(selector).ToArray();
        }
    }
}