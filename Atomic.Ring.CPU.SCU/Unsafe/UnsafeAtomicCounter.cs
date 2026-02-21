using System.Numerics;
using System.Runtime.CompilerServices;

namespace Atomic.Ring.CPU.SCU;

/// <summary>
/// High‑performance, Lock‑free, Thread‑safe and timing-stable counter that absolutely eliminates CPU 'false sharing'
/// </summary>
public class UnsafeAtomicCounter: IDisposable
{
    private readonly AlignedMemoryHandle _handle;
    private readonly uint _mask;
    
    private volatile int _disposed;

    /// <summary>
    /// Construct CPU CacheLine Ring of size Environment.ProcessorCount multiplied by 2 raised to the power of <paramref name="expansionFactor"/> (default value is 0)
    /// </summary>
    /// <param name="expansionFactor">default value is 0. This value doubles the ring length, big ring may accommodates some hyper-threading scenarios</param>
    /// <exception cref="ArgumentException">when <paramref name="expansionFactor"/> more than 8 or when <paramref name="ringLength"/> ≤ 2</exception>
    /// <exception cref="AlignedMemoryAllocationException">when memory allocation is not possible</exception>
    public UnsafeAtomicCounter(byte expansionFactor = 0) : this((uint)Environment.ProcessorCount, expansionFactor) { }

    /// <summary>
    /// Construct CPU CacheLine Ring of size <paramref name="ringLength"/> multiplied by 2 raised to the power of <paramref name="expansionFactor"/>
    /// </summary>
    /// <param name="ringLength">base size of ring</param>
    /// <param name="expansionFactor">This value doubles the ring length, big ring may accommodates some hyper-threading scenarios</param>
    /// <exception cref="ArgumentException">when <paramref name="expansionFactor"/> more than 8 or when <paramref name="ringLength"/> ≤ 2</exception>
    /// <exception cref="AlignedMemoryAllocationException">when memory allocation is not possible</exception>
    public UnsafeAtomicCounter(uint ringLength, byte expansionFactor)
    {
        if (expansionFactor > 8) throw new ArgumentException("ExpansionFactor exceeds maximum allowed", nameof(expansionFactor));
        if (ringLength == 0) throw new ArgumentException("Resulting ring length is 0", nameof(ringLength));

        ringLength = BitOperations.RoundUpToPowerOf2(ringLength);

        ulong expanded = (ulong)ringLength << expansionFactor;
        if (expanded > uint.MaxValue) throw new ArgumentException("Resulting ring length exceeds maximum allowed", nameof(ringLength));
        
        ringLength = (uint)expanded;
        if (ringLength <= 1) throw new ArgumentException("Resulting ring length must be at least 2.", nameof(ringLength));
        _mask = ringLength - 1;

        _handle = new AlignedMemoryHandle(ringLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckDisposed()
    {
        if (_disposed != 0) throw new ObjectDisposedException(GetType().Name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe long* GetElementPointer(uint indexInRing, IntPtr ptr) => (long*)((byte*)ptr + ((indexInRing & _mask) * AlignedMemoryHandle.CACHE_LINE_SIZE));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe long* GetElementPointer(uint indexInRing) => GetElementPointer(indexInRing, _handle.DangerousGetHandle());

    /// <summary>
    /// Increment as an atomic, lock-free & thread-safe operation. Uses the current thread's ManagedThreadId as the index
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown if the object has been disposed</exception>
    /// <returns>incremented value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long Increment() => Increment((uint)Environment.CurrentManagedThreadId);

    /// <summary>
    /// Increment as an atomic, lock-free & thread-safe operation.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown if the object has been disposed</exception>
    /// <param name="indexInRing">Index in ring</param>
    /// <returns>incremented value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long Increment(uint indexInRing)
    {
        CheckDisposed();
        unsafe
        {
            return Interlocked.Increment(ref *GetElementPointer(indexInRing));
        }
    }

    /// <summary>
    /// Decrement as an atomic, lock-free & thread-safe operation. Uses the current thread's ManagedThreadId as the index
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown if the object has been disposed</exception>
    /// <returns>decremented value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long Decrement() => Decrement((uint)Environment.CurrentManagedThreadId);

    /// <summary>
    /// Decrement as an atomic, lock-free & thread-safe operation.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown if the object has been disposed</exception>
    /// <param name="indexInRing">Index in ring</param>
    /// <returns>decremented value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long Decrement(uint indexInRing)
    {
        CheckDisposed();
        unsafe
        {
            return Interlocked.Decrement(ref *GetElementPointer(indexInRing));
        }
    }

    /// <summary>
    /// 'Add' as an atomic, lock-free & thread-safe operation. Uses the current thread's ManagedThreadId as the index
    /// </summary>
    /// <param name="value">(signed) value to be added</param>
    /// <exception cref="ObjectDisposedException">Thrown if the object has been disposed</exception>
    /// <returns>value after operation</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long Add(int value) => Add((uint)Environment.CurrentManagedThreadId, value);

    /// <summary>
    /// 'Add' as an atomic, lock-free & thread-safe operation.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown if the object has been disposed</exception>
    /// <param name="indexInRing">Index in ring</param>
    /// <param name="value">(signed) value to be added</param>
    /// <returns>value after operation</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long Add(uint indexInRing, int value)
    {
        CheckDisposed();
        unsafe
        {
            return Interlocked.Add(ref *GetElementPointer(indexInRing), value);
        }
    }

    /// <summary>
    /// Obtaining the result value of counter, raw value, it is sum of values in ring. Atomic, lock-free & thread-safe operation, result can be not fully consistent across all elements in ring for high load. 
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown if the object has been disposed</exception>
    /// <returns>sum of values in ring, it is resulted Value</returns>
    public long VolatileValue => GetVolatileValue();

    /// <summary>
    /// Obtaining the result value of counter, raw value, it is sum of values in ring. Atomic, lock-free & thread-safe operation, result can be not fully consistent across all elements in ring for high load. 
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown if the object has been disposed</exception>
    /// <returns>sum of values in ring, it is resulted Value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long GetVolatileValue()
    {
        CheckDisposed();
        long total = 0;
        unsafe
        {
            var ptr = _handle.DangerousGetHandle();
            for (uint i = 0; i < _handle.RingLength; i++) total += Volatile.Read(ref *GetElementPointer(i, ptr));
        }
        return total;
    }
    
    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0) return;
        _handle?.Dispose();
        GC.SuppressFinalize(this);
    }
}
