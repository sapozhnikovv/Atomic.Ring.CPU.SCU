using Benchmarks;
using System.Diagnostics;

namespace Tests
{
    public class UnitTest
    {
        public CounterBenchmark SetupBenchmarkObjectForTest()
        {
            var bench = new CounterBenchmark();
            bench.ExpansionFactor = 5;
            bench.OperationsPerThread = 1_000_000;
            bench.ThreadCount = 32;
            bench.Setup();//set min threads
            return bench;
        }
        [Fact]
        public Task TestSimpleCounter() => SetupBenchmarkObjectForTest().SimpleCounterTest();

        [Fact]
        public Task TestAtomicCounter() => SetupBenchmarkObjectForTest().AtomicCounterTest();

        [Fact]
        public Task SimplifiedAtomicCounterWithoutSpecificTypeTest() => SetupBenchmarkObjectForTest().SimplifiedAtomicCounterWithoutSpecificTypeTest();

        [Fact]
        public Task SimplifiedAtomicCounterWithSpecificTypeCloseToUnsafeVersionTest() => SetupBenchmarkObjectForTest().SimplifiedAtomicCounterWithSpecificTypeCloseToUnsafeVersionTest();

        [Fact]
        public async Task IsUnsafeAtomicFasterThanCommonManagedAtomic()
        {
            var bench = SetupBenchmarkObjectForTest();

            // WarmUp
            for (int i = 0; i < 10; i++)
            {
                await bench.SimplifiedAtomicCounterWithoutSpecificTypeTest();
                await bench.AtomicCounterTest();
            }

            // Measure
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++) await bench.SimplifiedAtomicCounterWithoutSpecificTypeTest();
            sw.Stop();
            var simplifiedTime = sw.Elapsed;

            sw.Restart();
            for (int i = 0; i < 100; i++) await bench.AtomicCounterTest();
            sw.Stop();
            var atomicTime = sw.Elapsed;

            Assert.True(atomicTime < simplifiedTime, $"UnsafeAtomic: {atomicTime.TotalMilliseconds} ms >= ManagedAtomic: {simplifiedTime.TotalMilliseconds} ms");
        }

        [Fact]
        public async Task IsUnsafeAtomicFasterOrEqualToSpecificTypeManagedAtomic()
        {
            var bench = SetupBenchmarkObjectForTest();

            // WarmUp
            for (int i = 0; i < 10; i++)
            {
                await bench.SimplifiedAtomicCounterWithSpecificTypeCloseToUnsafeVersionTest();
                await bench.AtomicCounterTest();
            }

            // Measure
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++) await bench.SimplifiedAtomicCounterWithSpecificTypeCloseToUnsafeVersionTest();
            sw.Stop();
            var simplifiedTime = sw.Elapsed;

            sw.Restart();
            for (int i = 0; i < 100; i++) await bench.AtomicCounterTest();
            sw.Stop();
            var atomicTime = sw.Elapsed;

            Assert.True(atomicTime <= simplifiedTime, $"UnsafeAtomic: {atomicTime.TotalMilliseconds} ms > ManagedAtomic: {simplifiedTime.TotalMilliseconds} ms");
        }

        [Fact]
        public async Task IsSpecificTypeManagedAtomicFasterOrEqualToCommonManagedAtomic()
        {
            var bench = SetupBenchmarkObjectForTest();

            // WarmUp
            for (int i = 0; i < 10; i++)
            {
                await bench.SimplifiedAtomicCounterWithSpecificTypeCloseToUnsafeVersionTest();
                await bench.SimplifiedAtomicCounterWithoutSpecificTypeTest();
            }

            // Measure
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++) await bench.SimplifiedAtomicCounterWithSpecificTypeCloseToUnsafeVersionTest();
            sw.Stop();
            var aTime = sw.Elapsed;

            sw.Restart();
            for (int i = 0; i < 100; i++) await bench.SimplifiedAtomicCounterWithoutSpecificTypeTest();
            sw.Stop();
            var bTime = sw.Elapsed;

            Assert.True(aTime <= bTime, $"SpecificTypeManagedAtomicCounter: {aTime.TotalMilliseconds} ms > CommonTypeManagedAtomic: {bTime.TotalMilliseconds} ms");
        }
    }
}