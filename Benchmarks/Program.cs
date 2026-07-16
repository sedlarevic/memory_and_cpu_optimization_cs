// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Running;
using Benchmarks;

BenchmarkRunner.Run<GeneratorBenchmarks>();
BenchmarkSwitcher
    .FromAssembly(typeof(GeneratorBenchmarks).Assembly)
    .Run(args);
