# [Atomic.Ring.CPU.SCU](https://github.com/sapozhnikovv/Atomic.Ring.CPU.SCU)   
![Logo](https://github.com/sapozhnikovv/Atomic.Ring.CPU.SCU/blob/main/img/name.jpg)   
   
**High‑performance, Lock‑free, Thread‑safe and timing-stable counter that absolutely eliminates CPU 'false sharing'**   
**13.8x faster than 'Interlocked'**   
   
## Solved Problem: CPU 'False Sharing'     
Modern CPUs store data in small fixed‑size chunks called **cache lines** (typically 64 bytes). When one core modifies a cache line, that line must be invalidated on all other cores – an expensive operation.   
**False sharing** happens when multiple threads modify different variables that happen to reside on the same cache line. The CPU is forced to constantly bounce that line between cores, even though the threads are using unrelated data. This can cripple performance in highly concurrent code.   
This solution eliminate false sharing by placing each counter on its own dedicated cache line inside a ring buffer. Threads are automatically assigned to distinct slots, guaranteeing that no two threads ever compete for the same line. The result is a lock‑free counter that scales linearly with the number of cores.      
   
# Nuget   
[![NuGet](https://img.shields.io/nuget/v/Atomic.Ring.CPU.SCU)](https://www.nuget.org/packages/Atomic.Ring.CPU.SCU)   
multi-target package:   
✅ .net7.0   
✅ .net8.0   
✅ .net9.0  
   
https://www.nuget.org/packages/Atomic.Ring.CPU.SCU   
   
```shell
dotnet add package Atomic.Ring.CPU.SCU
```
or
```shell
NuGet\Install-Package Atomic.Ring.CPU.SCU
```
   
## Features   

- Two implementations:
  - **`UnsafeAtomicCounter`** – uses aligned native memory (`NativeMemory.AlignedAlloc`) to **completely eliminate CPU 'false sharing'**   
    ✅ **Timing‑stable** – performance is consistently fast even under extreme contention    
    ⚠️ Implements `IDisposable` (native memory freed via `SafeHandle`), but Dispose is not very required because of SafeHandle specific   
  - **`ManagedAtomicCounterXX`** – fully managed, uses a `struct` with explicit padding to cache‑line size  
    ⚠️ **Reduces false sharing** but cannot fully eliminate it due to object header shift in managed arrays (4-8 bytes initial shift)     
    🚀 **No `IDisposable`** – pure managed code. Available in variants for 32, 64, 128, and 256‑byte cache lines   
  - Factory uses for not high-load cases. Better performance (close to the unsafe version) can be achieved by using concrete types instead of ManagedAtomicCounterBase from ManagedAtomicCounterFactory, the type should be determined by the ManagedAtomicCounterFactory.CACHE_LINE_SIZE or from CacheLine.CPU.SCU package    
- **Dynamic sizing** – default ring length = `Environment.ProcessorCount × 2^expansionFactor` (0 ≤ 'ExpansionFactor' ≤ 8). 'ExpansionFactor' is 0 by default   
- **Automatic thread distribution** – threads are assigned a slot based on `Environment.CurrentManagedThreadId` (faster than `Thread.CurrentThread.ManagedThreadId`). Yes. It is not a 100% guarantee of even distribution, but for large ring sizes it works very well      
- **Explicit indexing** – you can also specify the slot index manually   
- **`Add` method** – atomically add any signed integer value (not just ±1)   
   
## Performance   
   
Benchmarks were run on an **AMD Ryzen 7 1700 (8 cores / 16 threads)** with .NET 8   
Each test executes **100 000 operations per thread**, with one decrement per thread (expected total = `ThreadCount x (OperationsPerThread - 1)`)      
The table below shows **mean execution time** for different thread counts and expansion factors   
   
> ⚠️ **All measurements are in microseconds (μs).**     
> `ExpansionFactor = k` -> ring size = `16 x 2^k`.   
   
### Selected results (full data in [Benchmarks.CounterBenchmark-report.html](https://github.com/sapozhnikovv/Atomic.Ring.CPU.SCU/tree/main/Benchmarks/Benchmarks.CounterBenchmark-report.html) )   
   
| Method                                             | Expansion | Threads | Mean (μs) | vs Simple |
|----------------------------------------------------|-----------|--------:|----------:|----------:|
| `SimpleInterlockedCounter`(baseline)                | 0(not used)| 64      | 166 592   | 1.00x     |
| `UnsafeAtomicCounter`                               | 0         | 64      | 18 597    | **8.96x faster** |
| `UnsafeAtomicCounter`                               | 4         | 64      | 12 053    | **13.8x faster** |
| `ManagedAtomicCounter64` (via factory, base class) | 0         | 64      | 41 067    | 4.06x faster |
| `ManagedAtomicCounter64` (concrete type)           | 0         | 64      | 41 010    | 4.06x faster |
| `ManagedAtomicCounter64` (concrete type)           | 4         | 64      | 12 473    | **13.4x faster** |
   
**Key observations:**   
   
- **`UnsafeAtomicCounter`** is consistently the fastest, especially when the ring size matches the thread count. It is also **timing‑stable** – its performance varies very little across runs    
- **`ManagedAtomicCounterXX`** with a large ring (`expansionFactor ≥ 3`) approaches the speed of the unsafe version    
- **Using the concrete type** (e.g., `ManagedAtomicCounter64`) instead of the base class/interface improves performance significantly – up to **37%** in some scenarios. This is due to JIT devirtualisation and inlining     
   
### Unit test example (single run, Release mode)   
    
These numbers are **illustrative only** – unit tests are not reliable for performance measurements because of JIT warm‑up, caching, and scheduling variations. Always rely on **BenchmarkDotNet** results.    
     
![Tests](https://github.com/sapozhnikovv/Atomic.Ring.CPU.SCU/blob/main/img/tests.jpg)

| Test                                                       | Time   |
|------------------------------------------------------------|-------:|
| `TestSimpleCounter`                                        | 904 ms |
| `TestAtomicCounter` (`UnsafeAtomicCounter`)                | 63 ms  |
| `SimplifiedAtomicCounterWithSpecificType` (concrete)       | 65 ms  |
| `SimplifiedAtomicCounterWithoutSpecificType` (base class)  | 67 ms  |
    
After a few warm‑up iterations the numbers stabilise close to the benchmark values.   
    
## Usage   
    
### Unsafe version (recommended for maximum performance)    

```csharp
using Atomic.Ring.CPU.SCU;
    
// Default ring size = ProcessorCount × 2^0
using var counter = new UnsafeAtomicCounter();
    
// Automatic slot selection based on current thread
counter.Increment();
counter.Decrement();
counter.Add(10); // add any signed value
    
long total = counter.VolatileValue; // volatile sum
```
   
### Managed version (choose the right cache‑line size)    
   
```csharp
// Factory returns the appropriate type for your CPU cache line
var counterBase = ManagedAtomicCounterFactory.ConstructManagedAtomicCounter(expansionFactor: 3);

// For best performance, cast to the concrete type if you know your cache line size:
var concrete = (ManagedAtomicCounter64)counterBase;
concrete.Increment();
```
   
Or directly instantiate the desired type if you know the cache line size:   
   
```csharp
var counter = new ManagedAtomicCounter64(expansionFactor: 3);
counter.Add(5);
```
   
### Adjust ring size   
   
```csharp
// Ring size = ProcessorCount × 2^expansionFactor
// For 16 cores, expansionFactor = 3 gives 128 slots
var bigCounter = new UnsafeAtomicCounter(expansionFactor: 3);
```
    
### Manual indexing (only if you really need it)

```csharp
counter.Increment(index: 42);
counter.Decrement(index: 7);
counter.Add(index: 15, value: 100);
```
   
## Which one should I choose?   
   
- **`UnsafeAtomicCounter`** – if you need guaranteed CPU 'false‑sharing' elimination, maximum and stable performance. It is also **timing‑stable**, meaning its performance is highly predictable.   
- **`ManagedAtomicCounterXX`** – if you prefer to avoid explicit resource disposal (though `UnsafeAtomicCounter` implements `IDisposable`, it is trivial to use with `using`). Managed versions are **almost as fast** with a sufficiently large ring, and you can enjoy pure managed code.   
   
## License
Free MIT license (https://github.com/sapozhnikovv/Atomic.Ring.CPU.SCU/blob/main/LICENSE)
