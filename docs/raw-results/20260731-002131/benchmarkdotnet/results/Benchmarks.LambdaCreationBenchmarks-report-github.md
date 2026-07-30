```

BenchmarkDotNet v0.15.8, macOS Sequoia 15.6.1 (24G90) [Darwin 24.6.0]
Apple M1, 1 CPU, 8 logical and 8 physical cores
.NET SDK 9.0.302
  [Host]     : .NET 9.0.7 (9.0.7, 9.0.725.31616), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 9.0.7 (9.0.7, 9.0.725.31616), Arm64 RyuJIT armv8.0-a


```
| Method                     | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| StaticMethodGroupCreation  | 0.0009 ns | 0.0028 ns | 0.0025 ns | 0.0000 ns |     ? |       ? |      - |         - |           ? |
| NonCapturingLambdaCreation | 0.0000 ns | 0.0000 ns | 0.0000 ns | 0.0000 ns |     ? |       ? |      - |         - |           ? |
| CapturingLambdaCreation    | 8.7464 ns | 0.0331 ns | 0.0258 ns | 8.7414 ns |     ? |       ? | 0.0140 |      88 B |           ? |
