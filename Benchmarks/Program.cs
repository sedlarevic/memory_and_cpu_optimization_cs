using BenchmarkDotNet.Running;
using Benchmarks;

BenchmarkSwitcher
    .FromAssembly(
        typeof(GeneratorProfileBenchmarks)
            .Assembly)
    .Run(args);