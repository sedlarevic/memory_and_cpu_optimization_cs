using BenchmarkDotNet.Running;
using Benchmarks;

BenchmarkSwitcher
    .FromAssembly(typeof(GeneratorBenchmarks).Assembly)
    .Run(args);