using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.Collections.Generic
{
    /// <summary>
    /// Provides extension methods that enable use of <c>await foreach</c>
    /// with <see cref="IAsyncEnumerator{T}"/>.
    /// </summary>
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public static class AsyncEnumeratorAdapter
    {
        /// <exclude/>
        public readonly struct Dispose<T>
        {
            private readonly IAsyncEnumerator<T> m_enumerator;

            public Dispose(IAsyncEnumerator<T> enumerator)
            {
                m_enumerator = enumerator;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator() => m_enumerator;
        }

        /// <exclude/>
        public readonly struct KeepAlive<T>
        {
            private readonly IAsyncEnumerator<T> m_enumerator;

            public KeepAlive(IAsyncEnumerator<T> enumerator)
            {
                m_enumerator = enumerator;
            }

            public KeepAlive<T> GetAsyncEnumerator() => this;

            public T Current => m_enumerator.Current;
            public ValueTask<bool> MoveNextAsync() => m_enumerator.MoveNextAsync();
        }

        /// <exclude/>
        public readonly struct ConfiguredDispose<T>
        {
            private readonly IAsyncEnumerator<T> m_enumerator;
            private readonly bool m_continueOnCapturedContext;

            public ConfiguredDispose(IAsyncEnumerator<T> enumerator, bool continueOnCapturedContext)
            {
                m_enumerator = enumerator;
                m_continueOnCapturedContext = continueOnCapturedContext;
            }

            public ConfiguredDispose<T> GetAsyncEnumerator() => this;

            public T Current => m_enumerator.Current;
            public ConfiguredValueTaskAwaitable<bool> MoveNextAsync() => m_enumerator.MoveNextAsync().ConfigureAwait(m_continueOnCapturedContext);
            public ConfiguredValueTaskAwaitable DisposeAsync() => m_enumerator.DisposeAsync().ConfigureAwait(m_continueOnCapturedContext);
        }

        /// <exclude/>
        public readonly struct ConfiguredKeepAlive<T>
        {
            private readonly IAsyncEnumerator<T> m_enumerator;
            private readonly bool m_continueOnCapturedContext;

            public ConfiguredKeepAlive(IAsyncEnumerator<T> enumerator, bool continueOnCapturedContext)
            {
                m_enumerator = enumerator;
                m_continueOnCapturedContext = continueOnCapturedContext;
            }

            public ConfiguredKeepAlive<T> GetAsyncEnumerator() => this;

            public T Current => m_enumerator.Current;
            public ConfiguredValueTaskAwaitable<bool> MoveNextAsync() => m_enumerator.MoveNextAsync().ConfigureAwait(m_continueOnCapturedContext);
        }

        /// <summary>
        /// Enables use of <c>await foreach</c> with <see cref="IAsyncEnumerator{T}"/>.
        /// The enumerator is disposed after enumeration completes.
        /// </summary>
        public static Dispose<T> EnumerateAndDispose<T>(this IAsyncEnumerator<T> enumerator) =>
                  new Dispose<T>(enumerator);

        /// <summary>
        /// Enables use of <c>await foreach</c> with <see cref="IAsyncEnumerator{T}"/>.
        /// The enumerator is NOT disposed after enumeration completes.
        /// Callers are responsible for managing the enumerator's lifetime.
        /// </summary>
        public static KeepAlive<T> EnumerateAndKeepAlive<T>(this IAsyncEnumerator<T> enumerator) =>
                  new KeepAlive<T>(enumerator);

        /// <summary>
        /// Enables use of <c>await foreach</c> with <see cref="IAsyncEnumerator{T}"/>.
        /// The enumerator is disposed after enumeration completes.
        /// </summary>
        public static ConfiguredDispose<T> ConfigureEnumerateAndDispose<T>(this IAsyncEnumerator<T> enumerator, bool continueOnCapturedContext) =>
                  new ConfiguredDispose<T>(enumerator, continueOnCapturedContext);

        /// <summary>
        /// Enables use of <c>await foreach</c> with <see cref="IAsyncEnumerator{T}"/>.
        /// The enumerator is NOT disposed after enumeration completes.
        /// Callers are responsible for managing the enumerator's lifetime.
        /// </summary>
        public static ConfiguredKeepAlive<T> ConfigureEnumerateAndKeepAlive<T>(this IAsyncEnumerator<T> enumerator, bool continueOnCapturedContext) =>
                  new ConfiguredKeepAlive<T>(enumerator, continueOnCapturedContext);
    }

    /// <summary>
    /// Provides extension methods for <c>IAsyncEnumXxx</c>.
    /// </summary>
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public static class AsyncEnumeratorExtensions
    {
        sealed class WrappingAsyncEnumerable<T> : IAsyncEnumerable<T>
        {
            private readonly IEnumerable<T> m_enumerable;

            public WrappingAsyncEnumerable(IEnumerable<T> enumerable)
            {
                m_enumerable = enumerable;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                return new WrappingAsyncEnumerator<T>(m_enumerable.GetEnumerator(), cancellationToken);
            }
        }

        sealed class WrappingAsyncEnumerator<T> : IAsyncEnumerator<T>, IValueTaskSource<bool>
        {
            private readonly IEnumerator<T> m_enumerator;
            private readonly CancellationToken m_cancellationToken;

            public WrappingAsyncEnumerator(IEnumerator<T> enumerator, CancellationToken cancellationToken = default)
            {
                m_enumerator = enumerator;
                m_cancellationToken = cancellationToken;
            }

            public T Current => m_enumerator.Current;

            public ValueTask DisposeAsync()
            {
                m_enumerator.Dispose();
                return new ValueTask();
            }

            public ValueTask<bool> MoveNextAsync()
            {
                return m_cancellationToken.IsCancellationRequested ?
                    new ValueTask<bool>(this, 0) :
                    new ValueTask<bool>(m_enumerator.MoveNext());
            }

            public bool GetResult(short token)
            {
                m_cancellationToken.ThrowIfCancellationRequested();
                throw new InvalidOperationException();
            }

            public ValueTaskSourceStatus GetStatus(short token)
            {
                return ValueTaskSourceStatus.Canceled;
            }

            public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
            {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Wraps a synchronous <see cref="IEnumerable{T}"/> into an <see cref="IAsyncEnumerable{T}"/>.
        /// </summary>
        public static IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> enumerable) => new WrappingAsyncEnumerable<T>(enumerable);

        /// <summary>
        /// Wraps a synchronous <see cref="IEnumerable{T}"/> into an <see cref="IAsyncEnumerator{T}"/>.
        /// </summary>
        public static IAsyncEnumerator<T> ToAsyncEnumerator<T>(this IEnumerable<T> enumerable, CancellationToken cancellationToken = default) =>
            new WrappingAsyncEnumerator<T>(enumerable.GetEnumerator(), cancellationToken);

        /// <summary>
        /// Wraps a synchronous <see cref="IEnumerable{T}"/> into an <see cref="IAsyncEnumerator{T}"/>.
        /// </summary>
        public static IAsyncEnumerator<T> ToAsyncEnumerator<T>(this IEnumerator<T> enumerator, CancellationToken cancellationToken = default) =>
            new WrappingAsyncEnumerator<T>(enumerator, cancellationToken);
    }

    /// <summary>
    /// Provides an empty <see cref="IAsyncEnumerator{T}"/>.
    /// </summary>
    public static class AsyncEnumerator<T>
    {
        sealed class EmptyAsyncEnumerator : IAsyncEnumerator<T>
        {
            public T Current => throw new InvalidOperationException();
            public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(false);
            public ValueTask DisposeAsync() => new ValueTask();
        }

        /// <summary>
        /// Gets an empty <see cref="IAsyncEnumerator{T}"/>.
        /// </summary>
        public static readonly IAsyncEnumerator<T> Empty = new EmptyAsyncEnumerator();
    }
}