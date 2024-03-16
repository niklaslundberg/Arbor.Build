using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Arbor.Build.Core;

public sealed class FixedSizedQueue<T>
{
    private readonly object _lockObject = new();
    private readonly ConcurrentQueue<T> _queue = new();

    public int Limit { get; set; }

    public void Enqueue([DisallowNull] T obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        lock (_lockObject)
        {
            _queue.Enqueue(obj);

            while (_queue.Count > Limit && _queue.TryDequeue(out _))
            {
                Thread.SpinWait(10);
            }
        }
    }

    public void Clear()
    {
        lock (_lockObject)
        {
            while (!_queue.IsEmpty && _queue.TryDequeue(out _))
            {
                Thread.SpinWait(10);
            }
        }
    }

    public ImmutableArray<T> AllCurrentItems
    {
        get
        {
            lock (_lockObject)
            {
                if (_queue.IsEmpty)
                {
                    return ImmutableArray<T>.Empty;
                }

                return _queue.ToArray().ToImmutableArray();
            }
        }
    }
}