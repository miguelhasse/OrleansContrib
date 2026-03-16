using BenchmarkDotNet.Attributes;
using Microsoft.Kiota.Abstractions.Serialization;
using Orleans.Serialization.Kiota.Testing;

namespace Orleans.Serialization.Kiota.Benchmarks;

[MemoryDiagnoser]
public class KiotaCodecPerformanceBenchmarks
{
    private KiotaCodecHarness? _harness;
    private byte[] _serialized = [];
    private IParsable _value = default!;

    [ParamsAllValues]
    public KiotaCodecKind CodecKind { get; set; }

    [Params(false, true)]
    public bool Compression { get; set; }

    [ParamsAllValues]
    public GraphEntityKind EntityKind { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _harness = KiotaCodecHarnessFactory.Create(CodecKind, Compression);
        _value = GraphEntitySamples.Create(EntityKind);
        _serialized = _harness.ObjectSerializer.SerializeToArray(_value);
    }

    [GlobalCleanup]
    public void Cleanup() => _harness?.Dispose();

    [Benchmark]
    public byte[] Serialize() => _harness!.ObjectSerializer.SerializeToArray(_value);

    [Benchmark]
    public object Deserialize() => _harness!.ObjectSerializer.Deserialize(_serialized);

    [Benchmark]
    public object DeepCopy() => _harness!.ObjectDeepCopier.Copy(_value);
}
