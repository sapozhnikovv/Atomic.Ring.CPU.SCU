namespace Benchmarks;

public class SimpleCounter
{
    private long _value;
    public long Increment() => Interlocked.Increment(ref _value);
    public long Decrement() => Interlocked.Decrement(ref _value);
    public long VolatileValue => Interlocked.Read(ref _value);
}
