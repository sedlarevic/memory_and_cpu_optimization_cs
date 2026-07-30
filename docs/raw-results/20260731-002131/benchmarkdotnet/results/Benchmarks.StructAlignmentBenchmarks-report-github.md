```

BenchmarkDotNet v0.15.8, macOS Sequoia 15.6.1 (24G90) [Darwin 24.6.0]
Apple M1, 1 CPU, 8 logical and 8 physical cores
.NET SDK 9.0.302
  [Host]     : .NET 9.0.7 (9.0.7, 9.0.725.31616), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 9.0.7 (9.0.7, 9.0.725.31616), Arm64 RyuJIT armv8.0-a


```
| Method             | TargetCount | Mean        | Error     | StdDev   | Ratio | Allocated | Alloc Ratio |
|------------------- |------------ |------------:|----------:|---------:|------:|----------:|------------:|
| **PoorlyAligned**      | **5000**        |    **14.36 μs** |  **0.040 μs** | **0.034 μs** |  **1.00** |         **-** |          **NA** |
| OptimizedAlignment | 5000        |    14.33 μs |  0.021 μs | 0.016 μs |  1.00 |         - |          NA |
|                    |             |             |           |          |       |           |             |
| **PoorlyAligned**      | **100000**      |   **287.45 μs** |  **0.304 μs** | **0.253 μs** |  **1.00** |         **-** |          **NA** |
| OptimizedAlignment | 100000      |   287.61 μs |  0.325 μs | 0.271 μs |  1.00 |         - |          NA |
|                    |             |             |           |          |       |           |             |
| **PoorlyAligned**      | **1000000**     | **2,886.39 μs** | **10.432 μs** | **9.248 μs** |  **1.00** |         **-** |          **NA** |
| OptimizedAlignment | 1000000     | 2,882.23 μs |  5.034 μs | 4.204 μs |  1.00 |         - |          NA |
