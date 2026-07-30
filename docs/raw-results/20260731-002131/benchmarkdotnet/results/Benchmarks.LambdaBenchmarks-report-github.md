```

BenchmarkDotNet v0.15.8, macOS Sequoia 15.6.1 (24G90) [Darwin 24.6.0]
Apple M1, 1 CPU, 8 logical and 8 physical cores
.NET SDK 9.0.302
  [Host]     : .NET 9.0.7 (9.0.7, 9.0.725.31616), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 9.0.7 (9.0.7, 9.0.725.31616), Arm64 RyuJIT armv8.0-a


```
| Method              | TargetCount | Mean         | Error      | StdDev    | Ratio | Allocated | Alloc Ratio |
|-------------------- |------------ |-------------:|-----------:|----------:|------:|----------:|------------:|
| **ExplicitMethod**      | **5000**        |     **3.149 μs** |  **0.0036 μs** | **0.0028 μs** |  **1.00** |         **-** |          **NA** |
| MethodGroupDelegate | 5000        |     7.986 μs |  0.0062 μs | 0.0052 μs |  2.54 |         - |          NA |
| NonCapturingLambda  | 5000        |     3.613 μs |  0.0173 μs | 0.0153 μs |  1.15 |         - |          NA |
| CapturingLambda     | 5000        |     3.327 μs |  0.0062 μs | 0.0052 μs |  1.06 |         - |          NA |
|                     |             |              |            |           |       |           |             |
| **ExplicitMethod**      | **100000**      |    **63.900 μs** |  **0.1016 μs** | **0.0793 μs** |  **1.00** |         **-** |          **NA** |
| MethodGroupDelegate | 100000      |   160.235 μs |  0.8639 μs | 0.8081 μs |  2.51 |         - |          NA |
| NonCapturingLambda  | 100000      |    72.393 μs |  0.2762 μs | 0.2157 μs |  1.13 |         - |          NA |
| CapturingLambda     | 100000      |    66.834 μs |  0.1341 μs | 0.1047 μs |  1.05 |         - |          NA |
|                     |             |              |            |           |       |           |             |
| **ExplicitMethod**      | **1000000**     |   **638.806 μs** |  **1.2178 μs** | **1.0169 μs** |  **1.00** |         **-** |          **NA** |
| MethodGroupDelegate | 1000000     | 1,601.735 μs | 10.2619 μs | 9.0969 μs |  2.51 |         - |          NA |
| NonCapturingLambda  | 1000000     |   720.715 μs |  1.4979 μs | 1.3278 μs |  1.13 |         - |          NA |
| CapturingLambda     | 1000000     |   669.033 μs |  2.4058 μs | 1.8783 μs |  1.05 |         - |          NA |
