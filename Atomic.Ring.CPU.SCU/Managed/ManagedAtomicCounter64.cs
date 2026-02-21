using System.Runtime.CompilerServices;

namespace Atomic.Ring.CPU.SCU;

/// <summary>
/// Lock‑free, Thread‑safe and not fully time-stable counter. Without CPU 'false sharing' for some scenarios 
/// </summary>
public sealed class ManagedAtomicCounter64 : ManagedAtomicCounterBase
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit, Size = 64)]
    private struct PaddedCounter
    {
        [System.Runtime.InteropServices.FieldOffset(0)]
        public long Value;
    }
    private readonly PaddedCounter[] _ring;

    /// <summary>
    /// Construct CPU CacheLine Ring of size Environment.ProcessorCount multiplied by 2 raised to the power of <paramref name="expansionFactor"/> (default value is 0)
    /// </summary>
    /// <param name="expansionFactor">default value is 0. This value doubles the ring length, big ring may accommodates some hyper-threading scenarios</param>
    /// <exception cref="ArgumentException">when <paramref name="expansionFactor"/> more than 8 or when <paramref name="ringLength"/> ≤ 2</exception>
    public ManagedAtomicCounter64(byte expansionFactor = 0) : base(expansionFactor)
    {
        _ring = new PaddedCounter[_ringLength];
    }

    /// <summary>
    /// Construct CPU CacheLine Ring of size <paramref name="ringLength"/> multiplied by 2 raised to the power of <paramref name="expansionFactor"/>
    /// </summary>
    /// <param name="ringLength">base size of ring</param>
    /// <param name="expansionFactor">This value doubles the ring length, big ring may accommodates some hyper-threading scenarios</param>
    /// <exception cref="ArgumentException">when <paramref name="expansionFactor"/> more than 8 or when <paramref name="ringLength"/> ≤ 2</exception>
    public ManagedAtomicCounter64(uint ringLength, byte expansionFactor) : base(ringLength, expansionFactor) 
    { 
        _ring = new PaddedCounter[_ringLength]; 
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override ref long GetRefOnValueInRing(uint index) => ref _ring[index].Value;
}
