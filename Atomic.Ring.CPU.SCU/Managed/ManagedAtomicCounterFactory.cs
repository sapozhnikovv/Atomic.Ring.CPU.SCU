namespace Atomic.Ring.CPU.SCU;

/// <summary>
/// Factory for Lock‑free, Thread‑safe and not fully time-stable counter. Without CPU 'false sharing' for some scenarios.
/// Better performance (close to the unsafe version) can be achieved by using concrete types instead of ManagedAtomicCounterBase, the type should be determined by the CACHE_LINE_SIZE parameter
/// </summary>
public class ManagedAtomicCounterFactory
{
    public static readonly int CACHE_LINE_SIZE = CacheLine.CPU.SCU.CacheLine.Size;

    /// <summary>
    /// Factory for Lock‑free, Thread‑safe and not fully time-stable counter. Without CPU 'false sharing' for some scenarios.
    /// Better performance (close to the unsafe version) can be achieved by using concrete types instead of ManagedAtomicCounterBase, the type should be determined by the CACHE_LINE_SIZE parameter
    /// </summary>
    public static ManagedAtomicCounterBase ConstructManagedAtomicCounter(byte expansionFactor = 0) => CACHE_LINE_SIZE switch
    {
        32      => new ManagedAtomicCounter32(expansionFactor),
        64      => new ManagedAtomicCounter64(expansionFactor),
        128     => new ManagedAtomicCounter128(expansionFactor),
        256     => new ManagedAtomicCounter256(expansionFactor),
        _       => new ManagedAtomicCounter64(expansionFactor)
    };

    /// <summary>
    /// Factory for Lock‑free, Thread‑safe and not fully time-stable counter. Without CPU 'false sharing' for some scenarios.
    /// Better performance (close to the unsafe version) can be achieved by using concrete types instead of ManagedAtomicCounterBase, the type should be determined by the CACHE_LINE_SIZE parameter
    /// </summary>
    public static ManagedAtomicCounterBase ConstructManagedAtomicCounter(uint ringLength, byte expansionFactor) => CACHE_LINE_SIZE switch
    {
        32      => new ManagedAtomicCounter32(ringLength, expansionFactor),
        64      => new ManagedAtomicCounter64(ringLength, expansionFactor),
        128     => new ManagedAtomicCounter128(ringLength, expansionFactor),
        256     => new ManagedAtomicCounter256(ringLength, expansionFactor),
        _       => new ManagedAtomicCounter64(ringLength, expansionFactor)
    };
}