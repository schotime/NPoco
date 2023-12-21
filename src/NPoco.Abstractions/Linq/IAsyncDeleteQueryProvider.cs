using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace NPoco.Linq
{
    public interface IAsyncDeleteQueryProvider<T>
    {
        IAsyncDeleteQueryProvider<T> Where(Expression<Func<T, bool>> whereExpression);
        Task<int> Execute(CancellationToken cancellationToken = default);
    }
}