using Microsoft.Win32.SafeHandles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Atomic.Ring.CPU.SCU;

/// <summary>
/// SafeHandle wrapper. 
/// The main purpose is allocate block of memory without start padding (4-8 bytes) of arrays in managed code, to prevent false sharing on CPU cache line. 
/// Size = CacheLine.CPU.SCU.CacheLine.Size * ringLength
/// The allocated memory is aligned to the CPU cache line boundary
/// </summary>
internal sealed class AlignedMemoryHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public static readonly int CACHE_LINE_SIZE = CacheLine.CPU.SCU.CacheLine.Size;
    public readonly uint RingLength;
    public AlignedMemoryHandle(uint ringLength) : base(true)
    {
        if (ringLength <= 1) throw new ArgumentOutOfRangeException(nameof(ringLength));
        RingLength = ringLength;
        unsafe
        {
            var size = CACHE_LINE_SIZE * ringLength;
            void* ptr = NativeMemory.AlignedAlloc((nuint)size, (nuint)CACHE_LINE_SIZE);
            if (ptr == null) throw new AlignedMemoryAllocationException();
            Unsafe.InitBlockUnaligned(ptr, 0, (uint)size);
            SetHandle((IntPtr)ptr);
        }
    }

    protected override unsafe bool ReleaseHandle()
    {
        NativeMemory.AlignedFree((void*)handle);
        return true;
    }
}
