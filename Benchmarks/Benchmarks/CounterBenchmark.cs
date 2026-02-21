using Atomic.Ring.CPU.SCU;
using BenchmarkDotNet.Attributes;

namespace Benchmarks;

[ThreadingDiagnoser]
public class CounterBenchmark
{
    [Params(0, 1, 2, 3, 4)]
    public byte ExpansionFactor;

    [Params(2, 4, 8, 16, 32, 64)]
    public int ThreadCount;

    [Params(100_000)]
    public int OperationsPerThread;

    private long Expected => ((long)ThreadCount * OperationsPerThread)/*increments*/ - ThreadCount/*decrements*/;

    [GlobalSetup]
    public void Setup()
    {
        ThreadPool.SetMinThreads(256, 256);
    }

    [Benchmark(Baseline = true)]
    public async Task SimpleCounterTest()
    {
        var simpleCounter = new SimpleCounter();
        var barrier = new Barrier(ThreadCount);
        var tasks = new List<Task>();
        for (var x = 0; x < ThreadCount; x++)
        {
            var shift = x;
            var mid = OperationsPerThread / 2;
            tasks.Add(Task.Run(() =>
            {
                barrier.SignalAndWait();
                for (int i = 0; i < OperationsPerThread; i++)
                {
                    simpleCounter.Increment();
                    if (i % 100 == shift) _ = simpleCounter.VolatileValue;
                    if (i == mid + shift) simpleCounter.Decrement();
                }
            }));
        }
        await Task.WhenAll(tasks);
        if (simpleCounter.VolatileValue != Expected) throw new Exception("VolatileValue != Expected");
    }

    [Benchmark]
    public async Task AtomicCounterTest()
    {
        using var atomicCounter = new UnsafeAtomicCounter(ExpansionFactor);
        var barrier = new Barrier(ThreadCount);
        var tasks = new List<Task>();
        for (var x = 0; x < ThreadCount; x++)
        {
            var shift = x;
            var mid = OperationsPerThread / 2;
            tasks.Add(Task.Run(() =>
            {
                barrier.SignalAndWait();
                for (int i = 0; i < OperationsPerThread; i++)
                {
                    atomicCounter.Increment();
                    if (i % 100 == shift) _ = atomicCounter.VolatileValue;
                    if (i == mid + shift) atomicCounter.Decrement();
                }
            }));
        }
        await Task.WhenAll(tasks);
        if (atomicCounter.VolatileValue != Expected) throw new Exception("VolatileValue != Expected");
    }

    [Benchmark]
    public async Task SimplifiedAtomicCounterWithoutSpecificTypeTest()
    {
        var simplifiedAtomicCounterWithoutUnsafe = ManagedAtomicCounterFactory.ConstructManagedAtomicCounter(ExpansionFactor);
        var barrier = new Barrier(ThreadCount);
        var tasks = new List<Task>();
        for (var x = 0; x < ThreadCount; x++)
        {
            var shift = x;
            var mid = OperationsPerThread / 2;
            tasks.Add(Task.Run(() =>
            {
                barrier.SignalAndWait();
                for (int i = 0; i < OperationsPerThread; i++)
                {
                    simplifiedAtomicCounterWithoutUnsafe.Increment();
                    if (i % 100 == shift) _ = simplifiedAtomicCounterWithoutUnsafe.VolatileValue;
                    if (i == mid + shift) simplifiedAtomicCounterWithoutUnsafe.Decrement();
                }
            }));
        }
        await Task.WhenAll(tasks);
        if (simplifiedAtomicCounterWithoutUnsafe.VolatileValue != Expected) throw new Exception("VolatileValue != Expected");
    }

    [Benchmark]
    public async Task SimplifiedAtomicCounterWithSpecificTypeCloseToUnsafeVersionTest()
    {
        var simplifiedAtomicCounterWithoutUnsafe = (ManagedAtomicCounter64)ManagedAtomicCounterFactory.ConstructManagedAtomicCounter(ExpansionFactor);
        var barrier = new Barrier(ThreadCount);
        var tasks = new List<Task>();
        for (var x = 0; x < ThreadCount; x++)
        {
            var shift = x;
            var mid = OperationsPerThread / 2;
            tasks.Add(Task.Run(() =>
            {
                barrier.SignalAndWait();
                for (int i = 0; i < OperationsPerThread; i++)
                {
                    simplifiedAtomicCounterWithoutUnsafe.Increment();
                    if (i % 100 == shift) _ = simplifiedAtomicCounterWithoutUnsafe.VolatileValue;
                    if (i == mid + shift) simplifiedAtomicCounterWithoutUnsafe.Decrement();
                }
            }));
        }
        await Task.WhenAll(tasks);
        if (simplifiedAtomicCounterWithoutUnsafe.VolatileValue != Expected) throw new Exception("VolatileValue != Expected");
    }
}
