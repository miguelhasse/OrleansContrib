using Microsoft.Kiota.Abstractions.Serialization;

namespace Orleans.Serialization.Kiota.Tests;

internal sealed class KiotaTestSubModel : IParsable
{
    public Guid? Id { get; set; }
    public string? Label { get; set; }

    public static KiotaTestSubModel CreateFromDiscriminatorValue(IParseNode _) => new();

    public IDictionary<string, Action<IParseNode>> GetFieldDeserializers() =>
        new Dictionary<string, Action<IParseNode>>
        {
        { "id",    n => Id    = n.GetGuidValue()   },
        { "label", n => Label = n.GetStringValue() },
        };

    public void Serialize(ISerializationWriter writer)
    {
        writer.WriteGuidValue("id", Id);
        writer.WriteStringValue("label", Label);
    }

    public override bool Equals(object? obj) =>
        obj is KiotaTestSubModel other && Id == other.Id && Label == other.Label;

    public override int GetHashCode() => HashCode.Combine(Id, Label);
}
