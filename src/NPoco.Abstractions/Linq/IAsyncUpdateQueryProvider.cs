using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace NPoco.Linq
{
    public interface IAsyncUpdateQueryProvider<T>
    {
        IAsyncUpdateQueryProvider<T> Where(Expression<Func<T, bool>> whereExpression);
        IAsyncUpdateQueryProvider<T> ExcludeDefaults();
        IAsyncUpdateQueryProvider<T> OnlyFields(Expression<Func<T, object>> onlyFields);
        Task<int> Execute(T obj, CancellationToken cancellationToken = default);
    }
}
