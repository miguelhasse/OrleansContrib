using Microsoft.Kiota.Abstractions.Serialization;

namespace Orleans.Serialization.Kiota.Tests;

internal sealed class KiotaTestModel : IParsable
{
    public string? StringProp { get; set; }
    public int? IntProp { get; set; }
    public DateTimeOffset? DateProp { get; set; }
    public decimal? DecimalProp { get; set; }
    public TimeSpan? DurationProp { get; set; }
    public KiotaTestSubModel? Nested { get; set; }
    public List<string>? Tags { get; set; }

    public static KiotaTestModel CreateFromDiscriminatorValue(IParseNode _) => new();

    public IDictionary<string, Action<IParseNode>> GetFieldDeserializers() =>
        new Dictionary<string, Action<IParseNode>>
        {
        { "stringProp", n => StringProp = n.GetStringValue() },
        { "intProp",    n => IntProp    = n.GetIntValue() },
        { "dateProp",   n => DateProp   = n.GetDateTimeOffsetValue() },
        { "decimalProp", n => DecimalProp = n.GetDecimalValue() },
        { "durationProp", n => DurationProp = n.GetTimeSpanValue() },
        { "nested",     n => Nested     = n.GetObjectValue(KiotaTestSubModel.CreateFromDiscriminatorValue) },
        { "tags",       n => Tags       = n.GetCollectionOfPrimitiveValues<string>()?.ToList() },
        };

    public void Serialize(ISerializationWriter writer)
    {
        writer.WriteStringValue("stringProp", StringProp);
        writer.WriteIntValue("intProp", IntProp);
        writer.WriteDateTimeOffsetValue("dateProp", DateProp);
        writer.WriteDecimalValue("decimalProp", DecimalProp);
        writer.WriteTimeSpanValue("durationProp", DurationProp);
        writer.WriteObjectValue("nested", Nested);
        writer.WriteCollectionOfPrimitiveValues("tags", Tags);
    }

    public override bool Equals(object? obj) =>
        obj is KiotaTestModel other
        && StringProp == other.StringProp
        && IntProp == other.IntProp
        && DateProp == other.DateProp
        && DecimalProp == other.DecimalProp
        && DurationProp == other.DurationProp
        && Equals(Nested, other.Nested)
        && (Tags ?? []).SequenceEqual(other.Tags ?? []);

    public override int GetHashCode() => HashCode.Combine(StringProp, IntProp, DateProp, DecimalProp, DurationProp);
}
