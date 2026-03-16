using Microsoft.Kiota.Abstractions.Serialization;

namespace Orleans.Serialization.Kiota.Tests;

internal sealed class KiotaCollectionTestModel : IParsable
{
    public string? Name { get; set; }
    public KiotaCollectionItemModel? PrimaryItem { get; set; }
    public List<string>? Tags { get; set; }
    public List<KiotaCollectionItemModel>? Items { get; set; }
    public List<KiotaTestStatus?>? States { get; set; }
    public List<string>? EmptyTags { get; set; }
    public List<KiotaCollectionItemModel>? EmptyItems { get; set; }
    public List<KiotaTestStatus?>? EmptyStates { get; set; }

    public static KiotaCollectionTestModel CreateFromDiscriminatorValue(IParseNode _) => new();

    public IDictionary<string, Action<IParseNode>> GetFieldDeserializers() =>
        new Dictionary<string, Action<IParseNode>>
        {
            { "name", n => Name = n.GetStringValue() },
            { "primaryItem", n => PrimaryItem = n.GetObjectValue(KiotaCollectionItemModel.CreateFromDiscriminatorValue) },
            { "tags", n => Tags = n.GetCollectionOfPrimitiveValues<string>()?.ToList() },
            { "items", n => Items = n.GetCollectionOfObjectValues(KiotaCollectionItemModel.CreateFromDiscriminatorValue)?.ToList() },
            { "states", n => States = n.GetCollectionOfEnumValues<KiotaTestStatus>()?.ToList() },
            { "emptyTags", n => EmptyTags = n.GetCollectionOfPrimitiveValues<string>()?.ToList() },
            { "emptyItems", n => EmptyItems = n.GetCollectionOfObjectValues(KiotaCollectionItemModel.CreateFromDiscriminatorValue)?.ToList() },
            { "emptyStates", n => EmptyStates = n.GetCollectionOfEnumValues<KiotaTestStatus>()?.ToList() },
        };

    public void Serialize(ISerializationWriter writer)
    {
        writer.WriteStringValue("name", Name);
        writer.WriteObjectValue("primaryItem", PrimaryItem);
        writer.WriteCollectionOfPrimitiveValues("tags", Tags);
        writer.WriteCollectionOfObjectValues("items", Items);
        writer.WriteCollectionOfEnumValues("states", States);
        writer.WriteCollectionOfPrimitiveValues("emptyTags", EmptyTags);
        writer.WriteCollectionOfObjectValues("emptyItems", EmptyItems);
        writer.WriteCollectionOfEnumValues("emptyStates", EmptyStates);
    }

    public override bool Equals(object? obj) =>
        obj is KiotaCollectionTestModel other
        && Name == other.Name
        && Equals(PrimaryItem, other.PrimaryItem)
        && SequenceEqual(Tags, other.Tags)
        && SequenceEqual(Items, other.Items)
        && SequenceEqual(States, other.States)
        && SequenceEqual(EmptyTags, other.EmptyTags)
        && SequenceEqual(EmptyItems, other.EmptyItems)
        && SequenceEqual(EmptyStates, other.EmptyStates);

    public override int GetHashCode() => HashCode.Combine(Name, PrimaryItem);

    private static bool SequenceEqual<T>(IReadOnlyList<T>? left, IReadOnlyList<T>? right) =>
        (left, right) switch
        {
            (null, null) => true,
            (null, not null) => false,
            (not null, null) => false,
            _ => left!.Count == right!.Count && left.SequenceEqual(right),
        };
}
