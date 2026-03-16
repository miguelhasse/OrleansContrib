using BenchmarkDotNet.Attributes;
using Orleans.Serialization.Kiota.Testing;

namespace Orleans.Serialization.Kiota.Benchmarks;

[MemoryDiagnoser]
public class KiotaCodecCompressionBenchmarks
{
    private KiotaCodecHarness? _harness;
    private object _value = default!;

    [ParamsAllValues]
    public KiotaCodecKind CodecKind { get; set; }

    [Params(false, true)]
    public bool Compression { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _harness = KiotaCodecHarnessFactory.Create(CodecKind, Compression);
        _value = GraphEntitySamples.Create(GraphEntityKind.Message);
    }

    [GlobalCleanup]
    public void Cleanup() => _harness?.Dispose();

    [Benchmark]
    public byte[] SerializeLargeMessagePayload() => _harness!.ObjectSerializer.SerializeToArray(_value);
}
