using BenchmarkDotNet.Running;
using Benchmarks;

BenchmarkRunner.Run<CounterBenchmark>();
Console.ReadLine();
