namespace Atomic.Ring.CPU.SCU;

public sealed class AlignedMemoryAllocationException : Exception
{
    public AlignedMemoryAllocationException(): base("Failed to allocate aligned/padded memory") { }
    public AlignedMemoryAllocationException(string message): base(message) { }
    public AlignedMemoryAllocationException(string message, Exception innerException): base(message, innerException) { }
}
