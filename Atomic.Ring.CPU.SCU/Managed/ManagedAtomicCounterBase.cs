using System.Numerics;
using System.Runtime.CompilerServices;

namespace Atomic.Ring.CPU.SCU;

/// <summary>
/// Lock‑free, Thread‑safe and not fully time-stable counter. Without CPU 'false sharing' for some scenarios 
/// </summary>
public abstract class ManagedAtomicCounterBase
{
    protected readonly uint _ringLength;
    private readonly uint _mask;

    /// <summary>
    /// Construct CPU CacheLine Ring of size Environment.ProcessorCount multiplied by 2 raised to the power of <paramref name="expansionFactor"/> (default value is 0)
    /// </summary>
    /// <param name="expansionFactor">default value is 0. This value doubles the ring length, big ring may accommodates some hyper-threading scenarios</param>
    /// <exception cref="ArgumentException">when <paramref name="expansionFactor"/> more than 8 or when <paramref name="ringLength"/> ≤ 2</exception>
    public ManagedAtomicCounterBase(byte expansionFactor = 0) : this((uint)Environment.ProcessorCount, expansionFactor) { }

    /// <summary>
    /// Construct CPU CacheLine Ring of size <paramref name="ringLength"/> multiplied by 2 raised to the power of <paramref name="expansionFactor"/>
    /// </summary>
    /// <param name="ringLength">base size of ring</param>
    /// <param name="expansionFactor">This value doubles the ring length, big ring may accommodates some hyper-threading scenarios</param>
    /// <exception cref="ArgumentException">when <paramref name="expansionFactor"/> more than 8 or when <paramref name="ringLength"/> ≤ 2</exception>
    public ManagedAtomicCounterBase(uint ringLength, byte expansionFactor)
    {
        if (expansionFactor > 8) throw new ArgumentException("ExpansionFactor exceeds maximum allowed", nameof(expansionFactor));
        if (ringLength == 0) throw new ArgumentException("Resulting ring length is 0", nameof(ringLength));

        ringLength = BitOperations.RoundUpToPowerOf2(ringLength);

        ulong expanded = (ulong)ringLength << expansionFactor;
        if (expanded > uint.MaxValue) throw new ArgumentException("Resulting ring length exceeds maximum allowed", nameof(ringLength));

        ringLength = (uint)expanded;
        if (ringLength <= 1) throw new ArgumentException("Resulting ring length must be at least 2.", nameof(ringLength));
        _mask = ringLength - 1;
        _ringLength = ringLength;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint GetIndexInRing() => (uint)(Environment.CurrentManagedThreadId & _mask);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract ref long GetRefOnValueInRing(uint index);

    /// <summary>
    /// Increment as an atomic, lock-free & thread-safe operation. Uses the current thread's ManagedThreadId as the index
    /// </summary>
    /// <returns>incremented value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long Increment() => Increment(GetIndexInRing());

    /// <summary>
    /// Increment as an atomic, lock-free & thread-safe operation. Uses the current thread's ManagedThreadId as the index
    /// </summary>
    /// <param name="indexInRing">Index in ring</param>
    /// <returns>incremented value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long Increment(uint indexInRing) => Interlocked.Increment(ref GetRefOnValueInRing(indexInRing));

    /// <summary>
    /// Decrement as an atomic, lock-free & thread-safe operation. Uses the current thread's ManagedThreadId as the index
    /// </summary>
    /// <returns>decremented value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long Decrement() => Decrement(GetIndexInRing());

    /// <summary>
    /// Decrement as an atomic, lock-free & thread-safe operation. Uses the current thread's ManagedThreadId as the index
    /// </summary>
    /// <param name="indexInRing">Index in ring</param>
    /// <returns>decremented value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long Decrement(uint indexInRing) => Interlocked.Decrement(ref GetRefOnValueInRing(indexInRing));


    /// <summary>
    /// 'Add' as an atomic, lock-free & thread-safe operation. Uses the current thread's ManagedThreadId as the index
    /// </summary>
    /// <param name="value">(signed) value to be added</param>
    /// <returns>value after operation</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long Add(int value) => Add(GetIndexInRing(), value);

    /// <summary>
    /// 'Add' as an atomic, lock-free & thread-safe operation.
    /// </summary>
    /// <param name="indexInRing">Index in ring</param>
    /// <param name="value">(signed) value to be added</param>
    /// <returns>value after operation</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long Add(uint indexInRing, int value) => Interlocked.Add(ref GetRefOnValueInRing(indexInRing), value);

    /// <summary>
    /// Obtaining the result value of counter, raw value, it is sum of values in ring. Atomic, lock-free & thread-safe operation, result can be not fully consistent across all elements in ring for high load. 
    /// </summary>
    /// <returns>sum of values in ring, it is resulted Value</returns>
    public long VolatileValue => GetVolatileValue();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long GetVolatileValue()
    {
        long total = 0;
        for (uint i = 0; i < _ringLength; i++) total += Volatile.Read(ref GetRefOnValueInRing(i));
        return total;
    }
}
