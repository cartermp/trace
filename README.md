Run this with

```
dotnet run -c release
```

These are the results on my machine, yours will likely differ:

Creating a scene:

``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18363.720 (1909/November2018Update/19H2)
Intel Core i7-6700K CPU 4.00GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.300-preview-015048
  [Host]     : .NET Core 3.1.3 (CoreCLR 4.700.20.11803, CoreFX 4.700.20.12001), X64 RyuJIT DEBUG
  DefaultJob : .NET Core 3.1.3 (CoreCLR 4.700.20.11803, CoreFX 4.700.20.12001), X64 RyuJIT


```
|   Method |     Mean |   Error |  StdDev | Ratio |    Gen 0 |  Gen 1 | Gen 2 | Allocated |
|--------- |---------:|--------:|--------:|------:|---------:|-------:|------:|----------:|
| Original | 426.9 μs | 2.82 μs | 2.64 μs |  1.00 | 328.6133 | 1.4648 |     - | 1333.7 KB |
| Modified | 182.3 μs | 3.65 μs | 4.61 μs |  0.42 | 146.2402 | 0.9766 |     - | 558.21 KB |

Rendering:

``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18363.720 (1909/November2018Update/19H2)
Intel Core i7-6700K CPU 4.00GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.300-preview-015048
  [Host]     : .NET Core 3.1.3 (CoreCLR 4.700.20.11803, CoreFX 4.700.20.12001), X64 RyuJIT DEBUG
  DefaultJob : .NET Core 3.1.3 (CoreCLR 4.700.20.11803, CoreFX 4.700.20.12001), X64 RyuJIT


```
|   Method |     Mean |    Error |   StdDev |   Median | Ratio | RatioSD |       Gen 0 |      Gen 1 |    Gen 2 | Allocated |
|--------- |---------:|---------:|---------:|---------:|------:|--------:|------------:|-----------:|---------:|----------:|
| Original | 209.8 ms | 16.86 ms | 49.71 ms | 191.0 ms |  1.00 |    0.00 | 275250.0000 | 46000.0000 | 750.0000 | 1095.8 MB |
| Modified | 121.1 ms |  4.47 ms | 13.18 ms | 120.5 ms |  0.59 |    0.08 | 217200.0000 |   200.0000 | 200.0000 | 865.71 MB |

Both seem to represent a nice improvement. However, there are likely more things to be explored:

* Using `IsByRefLike` structs and the `Span<T>` type to elide bounds checking
* Hunting down more places where allocations are happening, since they are expensive