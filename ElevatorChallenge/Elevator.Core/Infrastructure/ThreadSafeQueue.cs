using System.Collections.Concurrent;

namespace Elevator.Core.Infrastructure;

/// <summary>
/// A thread-safe queue wrapper for generic usage
/// </summary>
public class ThreadSafeQueue<T>
{
    private readonly ConcurrentQueue<T> _queue = new();

    public void Enqueue(T item)
    {
        _queue.Enqueue(item);
    }

    public bool TryDequeue(out T item)
    {
        return _queue.TryDequeue(out item!);
    }
    
    public IEnumerable<T> GetAll()
    {
        return _queue.ToArray();
    }

    public int Count => _queue.Count;
    public bool IsEmpty => _queue.IsEmpty;
}
