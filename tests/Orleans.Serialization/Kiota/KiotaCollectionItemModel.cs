using Microsoft.Kiota.Abstractions.Serialization;

namespace Orleans.Serialization.Kiota.Tests;

internal sealed class KiotaCollectionItemModel : IParsable
{
    public Guid? Id { get; set; }
    public string? Label { get; set; }
    public KiotaTestStatus? Status { get; set; }
    public List<string>? Aliases { get; set; }

    public static KiotaCollectionItemModel CreateFromDiscriminatorValue(IParseNode _) => new();

    public IDictionary<string, Action<IParseNode>> GetFieldDeserializers() =>
        new Dictionary<string, Action<IParseNode>>
        {
            { "id", n => Id = n.GetGuidValue() },
            { "label", n => Label = n.GetStringValue() },
            { "status", n => Status = n.GetEnumValue<KiotaTestStatus>() },
            { "aliases", n => Aliases = n.GetCollectionOfPrimitiveValues<string>()?.ToList() },
        };

    public void Serialize(ISerializationWriter writer)
    {
        writer.WriteGuidValue("id", Id);
        writer.WriteStringValue("label", Label);
        writer.WriteEnumValue("status", Status);
        writer.WriteCollectionOfPrimitiveValues("aliases", Aliases);
    }

    public override bool Equals(object? obj) =>
        obj is KiotaCollectionItemModel other
        && Id == other.Id
        && Label == other.Label
        && Status == other.Status
        && (Aliases ?? []).SequenceEqual(other.Aliases ?? []);

    public override int GetHashCode() => HashCode.Combine(Id, Label, Status);
}
