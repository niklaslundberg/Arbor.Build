﻿using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading;
using JetBrains.Annotations;

namespace Arbor.Build.Core;

public sealed class FixedSizedQueue<T>
{
    private readonly object _lockObject = new object();
    private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();

    public int Limit { get; set; }

    public void Enqueue([NotNull] T obj)
    {
        if (obj == null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

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