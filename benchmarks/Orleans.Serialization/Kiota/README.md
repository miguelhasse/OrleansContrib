# Orleans.Serialization.Kiota.Benchmarks

BenchmarkDotNet benchmarks for the Orleans Kiota serialization codecs.

## Overview

This project benchmarks the three supported codecs:

- `KiotaJsonCodec`
- `KiotaMessagePackCodec`
- `KiotaMemoryPackCodec`

Benchmarks are parameterized to run with compression disabled and enabled across the current Graph entity sample set.

## Benchmark classes

| Benchmark | Purpose |
|---|---|
| `KiotaCodecPerformanceBenchmarks` | Measures `Serialize`, `Deserialize`, and `DeepCopy` across codecs, compression modes, and the current Graph sample set. |
| `KiotaCodecCompressionBenchmarks` | Measures compressed vs. uncompressed serialization across codec types and entity kinds, including payload size, compression ratio, and allocation reporting. |

## Run benchmarks

From the repository root:

```powershell
dotnet run --project .\benchmarks\Orleans.Serialization\Kiota\Orleans.Serialization.Kiota.Benchmarks.csproj -c Release -- --filter "*"
```

### Smaller subsets

```powershell
dotnet run --project .\benchmarks\Orleans.Serialization\Kiota\Orleans.Serialization.Kiota.Benchmarks.csproj -c Release -- --filter "*Compression*"
```

```powershell
dotnet run --project .\benchmarks\Orleans.Serialization\Kiota\Orleans.Serialization.Kiota.Benchmarks.csproj -c Release -- --filter "*Serialize*"
```

## Results

BenchmarkDotNet writes reports under the repository-level `BenchmarkDotNet.Artifacts` directory.

The most recent focused post-tuning validation run produced:

- `BenchmarkDotNet.Artifacts\results\Orleans.Serialization.Kiota.Benchmarks.KiotaCodecCompressionBenchmarks-report-github.md`
- `BenchmarkDotNet.Artifacts\results\Orleans.Serialization.Kiota.Benchmarks.KiotaCodecCompressionBenchmarks-report.csv`
- `BenchmarkDotNet.Artifacts\results\Orleans.Serialization.Kiota.Benchmarks.KiotaCodecCompressionBenchmarks-report.html`

The focused report covers `Serialize` across all three codec kinds for `User`, `Chat`, and `Team`, with compression disabled and enabled. It already includes payload-size columns, compression ratios, and allocation data.

`KiotaCodecCompressionBenchmarks` still adds comparison columns for `Collection Shape`, `Uncompressed Bytes`, `Compressed Bytes`, and `Compression Ratio` when you run the compression-focused filter.

Latest `Serialize` excerpt with compression disabled (`mean / allocated`):

| Entity | Json | MessagePack | MemoryPack |
|---|---|---|---|
| User | 10.01 us / 2.84 KB | 20.53 us / 4.16 KB | 15.27 us / 5.48 KB |
| Chat | 35.81 us / 9.49 KB | 47.77 us / 13.61 KB | 38.87 us / 15.66 KB |
| Team | 21.69 us / 6.18 KB | 41.94 us / 8.17 KB | 32.85 us / 10.53 KB |

Latest `Serialize` excerpt with compression enabled (`mean / allocated`):

| Entity | Json | MessagePack | MemoryPack |
|---|---|---|---|
| User | 39.08 us / 2.52 KB | 58.06 us / 3.71 KB | 77.77 us / 3.86 KB |
| Chat | 129.63 us / 5.63 KB | 146.86 us / 10.94 KB | 157.48 us / 10.73 KB |
| Team | 77.27 us / 4.63 KB | 112.24 us / 6.45 KB | 122.04 us / 6.66 KB |

Latest payload-size excerpt from the same focused run (`uncompressed -> compressed bytes / ratio`):

| Collection Shape | Entity | Json | MessagePack | MemoryPack |
|---|---|---|---|---|
| PrimitiveAndObjectCollections | User | 742 -> 382 B / 48.52% | 987 -> 508 B / 48.53% | 2290 -> 614 B / 73.19% |
| ConversationCollections | Chat | 5177 -> 1172 B / 77.36% | 4018 -> 1260 B / 68.64% | 6600 -> 1489 B / 77.44% |
| NestedAggregateCollections | Team | 2249 -> 641 B / 71.50% | 2556 -> 766 B / 70.03% | 4922 -> 911 B / 81.49% |

This final focused snapshot reflects the codec tuning work: MessagePack payload size is now much closer to Json, MemoryPack improved substantially from its earlier keyed-string baseline, and Json still leads on throughput across these representative entities.

## Related documentation

- [Repository overview](../../../README.md)
- [SystemTextJson package](../../../src/Orleans.Serialization/Kiota/SystemTextJson/README.md)
- [MessagePack package](../../../src/Orleans.Serialization/Kiota/MessagePack/README.md)
- [MemoryPack package](../../../src/Orleans.Serialization/Kiota/MemoryPack/README.md)
- [Kiota tests](../../../tests/Orleans.Serialization/Kiota/README.md)
