using BenchmarkDotNet.Attributes;
using Microsoft.Kiota.Abstractions.Serialization;
using Orleans.Serialization.Kiota.Testing;

namespace Orleans.Serialization.Kiota.Benchmarks;

[MemoryDiagnoser]
[Config(typeof(KiotaCompressionBenchmarkConfig))]
public class KiotaCodecCompressionBenchmarks
{
    private KiotaCodecHarness? _compressedHarness;
    private KiotaCodecHarness? _uncompressedHarness;
    private IParsable _value = default!;

    [ParamsAllValues]
    public GraphEntityKind EntityKind { get; set; }

    [ParamsAllValues]
    public KiotaCodecKind CodecKind { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _compressedHarness = KiotaCodecHarnessFactory.Create(CodecKind, compression: true);
        _uncompressedHarness = KiotaCodecHarnessFactory.Create(CodecKind, compression: false);
        _value = GraphEntitySamples.Create(EntityKind);
        _ = CompressionMetricsCache.Get(CodecKind, EntityKind);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _compressedHarness?.Dispose();
        _uncompressedHarness?.Dispose();
    }

    [Benchmark]
    public byte[] SerializeUncompressed() => _uncompressedHarness!.ObjectSerializer.SerializeToArray(_value);

    [Benchmark]
    public byte[] SerializeCompressed() => _compressedHarness!.ObjectSerializer.SerializeToArray(_value);
}
