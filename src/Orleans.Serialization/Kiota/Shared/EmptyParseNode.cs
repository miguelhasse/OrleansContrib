using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;

namespace Orleans.Serialization;

internal sealed class EmptyParseNode : IParseNode
{
    public static EmptyParseNode Instance { get; } = new();

    private EmptyParseNode()
    {
    }

    public Action<IParsable>? OnBeforeAssignFieldValues { get; set; }

    public Action<IParsable>? OnAfterAssignFieldValues { get; set; }

    public string? GetStringValue() => null;

    public bool? GetBoolValue() => null;

    public byte? GetByteValue() => null;

    public sbyte? GetSbyteValue() => null;

    public int? GetIntValue() => null;

    public long? GetLongValue() => null;

    public float? GetFloatValue() => null;

    public double? GetDoubleValue() => null;

    public decimal? GetDecimalValue() => null;

    public Guid? GetGuidValue() => null;

    public DateTimeOffset? GetDateTimeOffsetValue() => null;

    public TimeSpan? GetTimeSpanValue() => null;

    public Date? GetDateValue() => null;

    public Time? GetTimeValue() => null;

    public byte[]? GetByteArrayValue() => null;

    public IParseNode? GetChildNode(string identifier) => null;

    public T GetObjectValue<T>(ParsableFactory<T> factory) where T : IParsable => factory(this);

    public IEnumerable<T> GetCollectionOfObjectValues<T>(ParsableFactory<T> factory) where T : IParsable => [];

    public IEnumerable<T> GetCollectionOfPrimitiveValues<T>() => [];

    public IEnumerable<T?> GetCollectionOfEnumValues<T>() where T : struct, Enum => [];

    public T? GetEnumValue<T>() where T : struct, Enum => null;
}
