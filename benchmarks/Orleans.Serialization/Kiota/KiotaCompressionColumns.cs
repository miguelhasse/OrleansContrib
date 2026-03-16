using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Orleans.Serialization.Kiota.Testing;
using System.Collections.Concurrent;
using System.Globalization;

namespace Orleans.Serialization.Kiota.Benchmarks;

internal sealed class KiotaCompressionBenchmarkConfig : ManualConfig
{
    public KiotaCompressionBenchmarkConfig()
    {
        AddColumnProvider(DefaultColumnProviders.Instance);
        AddColumn(new PayloadSizeColumn(compressed: false));
        AddColumn(new PayloadSizeColumn(compressed: true));
        AddColumn(new CompressionRatioColumn());
        Orderer = new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest);
    }
}

internal readonly record struct CompressionMetricKey(KiotaCodecKind CodecKind, GraphEntityKind EntityKind);

internal readonly record struct CompressionMetrics(int UncompressedSizeBytes, int CompressedSizeBytes)
{
    public double CompressionRatio => UncompressedSizeBytes == 0
        ? 0d
        : 1d - ((double)CompressedSizeBytes / UncompressedSizeBytes);
}

internal static class CompressionMetricsCache
{
    private static readonly ConcurrentDictionary<CompressionMetricKey, CompressionMetrics> s_cache = new();

    public static CompressionMetrics Get(KiotaCodecKind codecKind, GraphEntityKind entityKind) =>
        s_cache.GetOrAdd(new(codecKind, entityKind), static key =>
        {
            using var uncompressedHarness = KiotaCodecHarnessFactory.Create(key.CodecKind, compression: false);
            using var compressedHarness = KiotaCodecHarnessFactory.Create(key.CodecKind, compression: true);

            var value = GraphEntitySamples.Create(key.EntityKind);
            var uncompressed = uncompressedHarness.ObjectSerializer.SerializeToArray(value);
            var compressed = compressedHarness.ObjectSerializer.SerializeToArray(value);

            return new CompressionMetrics(uncompressed.Length, compressed.Length);
        });
}

internal sealed class PayloadSizeColumn(bool compressed) : IColumn
{
    public string Id => compressed ? "KiotaCompressedPayloadBytes" : "KiotaUncompressedPayloadBytes";
    public string ColumnName => compressed ? "Compressed Bytes" : "Uncompressed Bytes";
    public string Legend => compressed
        ? "Serialized payload size with compression enabled."
        : "Serialized payload size with compression disabled.";
    public bool AlwaysShow => true;
    public ColumnCategory Category => ColumnCategory.Custom;
    public int PriorityInCategory => compressed ? 1 : 0;
    public bool IsNumeric => true;
    public UnitType UnitType => UnitType.Dimensionless;

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase) => GetValue(summary, benchmarkCase, SummaryStyle.Default);

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style)
    {
        var metrics = GetMetrics(benchmarkCase);
        var size = compressed ? metrics.CompressedSizeBytes : metrics.UncompressedSizeBytes;
        return size.ToString(CultureInfo.InvariantCulture);
    }

    public bool IsAvailable(Summary summary) => true;

    public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

    private static CompressionMetrics GetMetrics(BenchmarkCase benchmarkCase) =>
        CompressionMetricsCache.Get(
            (KiotaCodecKind)benchmarkCase.Parameters[nameof(KiotaCodecCompressionBenchmarks.CodecKind)]!,
            (GraphEntityKind)benchmarkCase.Parameters[nameof(KiotaCodecCompressionBenchmarks.EntityKind)]!);
}

internal sealed class CompressionRatioColumn : IColumn
{
    public string Id => "KiotaCompressionRatio";
    public string ColumnName => "Compression Ratio";
    public string Legend => "Relative payload size reduction when compression is enabled.";
    public bool AlwaysShow => true;
    public ColumnCategory Category => ColumnCategory.Custom;
    public int PriorityInCategory => 2;
    public bool IsNumeric => true;
    public UnitType UnitType => UnitType.Dimensionless;

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase) => GetValue(summary, benchmarkCase, SummaryStyle.Default);

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style)
    {
        var metrics = CompressionMetricsCache.Get(
            (KiotaCodecKind)benchmarkCase.Parameters[nameof(KiotaCodecCompressionBenchmarks.CodecKind)]!,
            (GraphEntityKind)benchmarkCase.Parameters[nameof(KiotaCodecCompressionBenchmarks.EntityKind)]!);

        return metrics.CompressionRatio.ToString("P2", CultureInfo.InvariantCulture);
    }

    public bool IsAvailable(Summary summary) => true;

    public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
}
