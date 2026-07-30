```

BenchmarkDotNet v0.15.8, macOS Sequoia 15.6.1 (24G90) [Darwin 24.6.0]
Apple M1, 1 CPU, 8 logical and 8 physical cores
.NET SDK 9.0.302
  [Host]     : .NET 9.0.7 (9.0.7, 9.0.725.31616), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 9.0.7 (9.0.7, 9.0.725.31616), Arm64 RyuJIT armv8.0-a


```
| Method                    | TargetCount | Mean         | Error      | StdDev     | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------- |------------ |-------------:|-----------:|-----------:|------:|--------:|----------:|------------:|
| **TryCatchAroundLoopNoThrow** | **5000**        |     **5.113 μs** |  **0.1002 μs** |  **0.1832 μs** |  **1.02** |    **0.04** |         **-** |          **NA** |
| WithoutTryCatch           | 5000        |     5.022 μs |  0.0988 μs |  0.1249 μs |  1.00 |    0.03 |         - |          NA |
| TryCatchPerItemNoThrow    | 5000        |     6.576 μs |  0.1262 μs |  0.1181 μs |  1.31 |    0.04 |         - |          NA |
|                           |             |              |            |            |       |         |           |             |
| **TryCatchAroundLoopNoThrow** | **100000**      |    **98.520 μs** |  **1.1369 μs** |  **1.0634 μs** |  **1.00** |    **0.02** |         **-** |          **NA** |
| WithoutTryCatch           | 100000      |    98.469 μs |  1.5969 μs |  1.4938 μs |  1.00 |    0.02 |         - |          NA |
| TryCatchPerItemNoThrow    | 100000      |   131.351 μs |  2.4220 μs |  2.1470 μs |  1.33 |    0.03 |         - |          NA |
|                           |             |              |            |            |       |         |           |             |
| **TryCatchAroundLoopNoThrow** | **1000000**     |   **983.141 μs** | **19.3380 μs** | **18.0887 μs** |  **1.01** |    **0.02** |         **-** |          **NA** |
| WithoutTryCatch           | 1000000     |   969.438 μs | 10.3106 μs |  8.6098 μs |  1.00 |    0.01 |         - |          NA |
| TryCatchPerItemNoThrow    | 1000000     | 1,304.331 μs | 16.9455 μs | 15.0217 μs |  1.35 |    0.02 |         - |          NA |
