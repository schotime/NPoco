using System;
using System.Threading;
using System.Threading.Tasks;

namespace NPoco
{
    public interface IAsyncTransaction : IAsyncDisposable, IDisposable
    {
        Task CompleteAsync(CancellationToken cancellationToken = default);
    }
}