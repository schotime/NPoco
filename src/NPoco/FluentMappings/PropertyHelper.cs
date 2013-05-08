using System;
using System.Linq.Expressions;
using System.Reflection;

namespace NPoco.FluentMappings
{
    public static class PropertyHelper<T>
    {
        public static MemberInfo GetProperty<TValue>(Expression<Func<T, TValue>> selector)
        {
            Expression body = selector;
            if (body is LambdaExpression)
            {
                body = ((LambdaExpression)body).Body;
            }
            if (body is UnaryExpression)
            {
                body = ((UnaryExpression)body).Operand;
            }
            switch (body.NodeType)
            {
                case ExpressionType.MemberAccess:
                    return ((MemberExpression)body).Member;
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}