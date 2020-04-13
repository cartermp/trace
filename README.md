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
|   Method |     Mean |   Error |  StdDev | Ratio |    Gen 0 |   Gen 1 | Gen 2 |  Allocated |
|--------- |---------:|--------:|--------:|------:|---------:|--------:|------:|-----------:|
| Original | 438.6 μs | 7.10 μs | 6.29 μs |  1.00 | 329.1016 | 23.4375 |     - | 1333.62 KB |
| Modified | 178.0 μs | 3.53 μs | 5.06 μs |  0.41 | 146.7285 |  0.4883 |     - |  558.04 KB |

Rendering:

``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18363.720 (1909/November2018Update/19H2)
Intel Core i7-6700K CPU 4.00GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.300-preview-015048
  [Host]     : .NET Core 3.1.3 (CoreCLR 4.700.20.11803, CoreFX 4.700.20.12001), X64 RyuJIT DEBUG
  DefaultJob : .NET Core 3.1.3 (CoreCLR 4.700.20.11803, CoreFX 4.700.20.12001), X64 RyuJIT


```
|   Method |      Mean |    Error |   StdDev | Ratio | RatioSD |       Gen 0 |      Gen 1 |     Gen 2 |  Allocated |
|--------- |----------:|---------:|---------:|------:|--------:|------------:|-----------:|----------:|-----------:|
| Original | 178.68 ms | 3.550 ms | 9.718 ms |  1.00 |    0.00 | 275000.0000 | 46000.0000 | 1000.0000 | 1095.79 MB |
| Modified |  63.01 ms | 0.768 ms | 0.681 ms |  0.35 |    0.02 |   2555.5556 |   111.1111 |  111.1111 |    10.8 MB |

Both seem to represent a nice improvement. However, there are likely more things to be explored:

* Using `IsByRefLike` structs and the `Span<T>` type to elide bounds checking
* Hunting down more places where allocations are happening, since they are expensive