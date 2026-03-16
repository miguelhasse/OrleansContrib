using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions.Serialization;
using Orleans.Serialization.Cloning;
using Orleans.Serialization.Serializers;
using System.Text;

namespace Orleans.Serialization.Kiota.Testing;

public enum KiotaCodecKind
{
    Json,
    MessagePack,
    MemoryPack,
}

public enum GraphEntityKind
{
    User,
    Message,
    Chat,
    ChatMessage,
    Event,
    Group,
    Contact,
    DriveItem,
    Team,
}

public enum GraphEntityCollectionShape
{
    PrimitiveAndObjectCollections,
    AttachmentHeavyCollections,
    ConversationCollections,
    InteractionCollections,
    SchedulingCollections,
    DirectoryCollections,
    MostlyPrimitiveCollections,
    HierarchicalCollections,
    NestedAggregateCollections,
}

public sealed class KiotaCodecHarness : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    internal KiotaCodecHarness(ServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        ObjectSerializer = _serviceProvider.GetRequiredService<Serializer<object>>();
        ObjectDeepCopier = _serviceProvider.GetRequiredService<DeepCopier<object>>();
    }

    public Serializer<object> ObjectSerializer { get; }

    public DeepCopier<object> ObjectDeepCopier { get; }

    public T GetRequiredService<T>() where T : notnull => _serviceProvider.GetRequiredService<T>();

    public void Dispose() => _serviceProvider.Dispose();
}

public static class KiotaCodecHarnessFactory
{
    public static KiotaCodecHarness Create(KiotaCodecKind codecKind, bool compression)
    {
        var services = new ServiceCollection();
        services.AddSerializer(serializerBuilder => Register(serializerBuilder.Services, codecKind, compression));

        return new KiotaCodecHarness(services.BuildServiceProvider());
    }

    public static void Register(IServiceCollection services, KiotaCodecKind codecKind, bool compression)
    {
        switch (codecKind)
        {
            case KiotaCodecKind.Json:
                AddCodec<KiotaJsonCodec, KiotaJsonCodecOptions>(services, options => options.Compression = compression);
                break;
            case KiotaCodecKind.MessagePack:
                AddCodec<KiotaMessagePackCodec, KiotaMessagePackOptions>(services, options => options.Compression = compression);
                break;
            case KiotaCodecKind.MemoryPack:
                AddCodec<KiotaMemoryPackCodec, KiotaMemoryPackOptions>(services, options => options.Compression = compression);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(codecKind), codecKind, "Unknown Kiota codec kind.");
        }
    }

    private static void AddCodec<TCodec, TOptions>(IServiceCollection services, Action<TOptions> configure)
        where TCodec : class, IGeneralizedCodec, IGeneralizedCopier, ITypeFilter
        where TOptions : class, new()
    {
        services.Configure(configure);
        services.AddSingleton<TCodec>();
        services.AddSingleton<IGeneralizedCodec>(serviceProvider => serviceProvider.GetRequiredService<TCodec>());
        services.AddSingleton<IGeneralizedCopier>(serviceProvider => serviceProvider.GetRequiredService<TCodec>());
        services.AddSingleton<ITypeFilter>(serviceProvider => serviceProvider.GetRequiredService<TCodec>());
    }
}

public static class GraphEntitySamples
{
    public static IParsable Create(GraphEntityKind entityKind) => entityKind switch
    {
        GraphEntityKind.User => CreateUser(),
        GraphEntityKind.Message => CreateMessage(),
        GraphEntityKind.Chat => CreateChat(),
        GraphEntityKind.ChatMessage => CreateChatMessage(),
        GraphEntityKind.Event => CreateEvent(),
        GraphEntityKind.Group => CreateGroup(),
        GraphEntityKind.Contact => CreateContact(),
        GraphEntityKind.DriveItem => CreateDriveItem(),
        GraphEntityKind.Team => CreateTeam(),
        _ => throw new ArgumentOutOfRangeException(nameof(entityKind), entityKind, "Unknown graph entity kind."),
    };

    public static GraphEntityCollectionShape GetCollectionShape(GraphEntityKind entityKind) => entityKind switch
    {
        GraphEntityKind.User => GraphEntityCollectionShape.PrimitiveAndObjectCollections,
        GraphEntityKind.Message => GraphEntityCollectionShape.AttachmentHeavyCollections,
        GraphEntityKind.Chat => GraphEntityCollectionShape.ConversationCollections,
        GraphEntityKind.ChatMessage => GraphEntityCollectionShape.InteractionCollections,
        GraphEntityKind.Event => GraphEntityCollectionShape.SchedulingCollections,
        GraphEntityKind.Group => GraphEntityCollectionShape.DirectoryCollections,
        GraphEntityKind.Contact => GraphEntityCollectionShape.MostlyPrimitiveCollections,
        GraphEntityKind.DriveItem => GraphEntityCollectionShape.HierarchicalCollections,
        GraphEntityKind.Team => GraphEntityCollectionShape.NestedAggregateCollections,
        _ => throw new ArgumentOutOfRangeException(nameof(entityKind), entityKind, "Unknown graph entity kind."),
    };

    public static User CreateUser() =>
        new()
        {
            OdataType = "#microsoft.graph.user",
            Id = "user-ada-lovelace",
            DisplayName = "Ada Lovelace",
            GivenName = "Ada",
            Surname = "Lovelace",
            Mail = "ada.lovelace@contoso.example",
            BusinessPhones =
            [
                "+1 425 555 0100",
                "+1 425 555 0101",
            ],
            Identities =
            [
                new ObjectIdentity
                {
                    SignInType = "emailAddress",
                    Issuer = "contoso.example",
                    IssuerAssignedId = "ada.lovelace@contoso.example",
                },
                new ObjectIdentity
                {
                    SignInType = "federated",
                    Issuer = "fabrikam.example",
                    IssuerAssignedId = "ada-lovelace-fabrikam",
                },
            ],
            EmployeeOrgData = new EmployeeOrgData
            {
                Division = "Engineering",
                CostCenter = "RND-042",
            },
            Manager = new User
            {
                OdataType = "#microsoft.graph.user",
                Id = "user-charles-babbage",
                DisplayName = "Charles Babbage",
                Mail = "charles.babbage@contoso.example",
            },
        };

    public static Message CreateMessage() =>
        new()
        {
            OdataType = "#microsoft.graph.message",
            Id = "message-quarterly-review",
            Subject = "Quarterly architecture review",
            Body = new ItemBody
            {
                Content = CreateLongText(
                    "The Orleans + Kiota integration review covers serialization fidelity, message throughput, and payload stability.",
                    10),
            },
            From = CreateRecipient("Ada Lovelace", "ada.lovelace@contoso.example"),
            ToRecipients =
            [
                CreateRecipient("Grace Hopper", "grace.hopper@contoso.example"),
                CreateRecipient("Margaret Hamilton", "margaret.hamilton@contoso.example"),
            ],
            ReplyTo =
            [
                CreateRecipient("Architecture Guild", "architecture-guild@contoso.example"),
            ],
            InternetMessageHeaders =
            [
                new InternetMessageHeader
                {
                    Name = "X-Environment",
                    Value = "Integration",
                },
                new InternetMessageHeader
                {
                    Name = "X-Orleans-Codec",
                    Value = "Kiota",
                },
            ],
            Attachments =
            [
                new FileAttachment
                {
                    OdataType = "#microsoft.graph.fileAttachment",
                    Name = "review-notes.txt",
                    ContentType = "text/plain",
                    ContentBytes = Encoding.UTF8.GetBytes(CreateLongText("Serialized attachment content.", 24)),
                },
                new FileAttachment
                {
                    OdataType = "#microsoft.graph.fileAttachment",
                    Name = "agenda.txt",
                    ContentType = "text/plain",
                    ContentBytes = Encoding.UTF8.GetBytes(CreateLongText("Agenda item.", 16)),
                },
            ],
        };

    public static Event CreateEvent() =>
        new()
        {
            OdataType = "#microsoft.graph.event",
            Id = "event-architecture-sync",
            Subject = "Architecture sync",
            Body = new ItemBody
            {
                Content = CreateLongText("Discussion topics include codec registration, payload compression, and benchmark baselines.", 8),
            },
            Start = new DateTimeTimeZone
            {
                DateTime = "2026-03-15T09:30:00",
                TimeZone = "UTC",
            },
            End = new DateTimeTimeZone
            {
                DateTime = "2026-03-15T10:30:00",
                TimeZone = "UTC",
            },
            Organizer = CreateRecipient("Ada Lovelace", "ada.lovelace@contoso.example"),
            Attendees =
            [
                new Attendee
                {
                    EmailAddress = new EmailAddress
                    {
                        Name = "Grace Hopper",
                        Address = "grace.hopper@contoso.example",
                    },
                },
                new Attendee
                {
                    EmailAddress = new EmailAddress
                    {
                        Name = "Margaret Hamilton",
                        Address = "margaret.hamilton@contoso.example",
                    },
                },
            ],
            Locations =
            [
                new Location
                {
                    DisplayName = "Virtual room",
                    Address = new PhysicalAddress
                    {
                        Street = "2 Collaboration Ave",
                        City = "Seattle",
                        CountryOrRegion = "USA",
                        PostalCode = "98101",
                    },
                },
            ],
            Location = new Location
            {
                DisplayName = "Building 1 / Room 42",
                Address = new PhysicalAddress
                {
                    Street = "1 Developer Way",
                    City = "Redmond",
                    CountryOrRegion = "USA",
                    PostalCode = "98052",
                },
            },
        };

    public static Group CreateGroup() =>
        new()
        {
            OdataType = "#microsoft.graph.group",
            Id = "group-platform-serialization",
            DisplayName = "Platform Serialization Guild",
            Description = CreateLongText("A working group focused on serialization, interoperability, and codec benchmarking.", 6),
            MailNickname = "platform-serialization",
            MailEnabled = true,
            SecurityEnabled = false,
            GroupTypes =
            [
                "Unified",
                "DynamicMembership",
            ],
            Members =
            [
                CreateMemberUser("member-alan-turing", "Alan Turing"),
                CreateMemberUser("member-donald-knuth", "Donald Knuth"),
            ],
            Owners =
            [
                CreateMemberUser("owner-katherine-johnson", "Katherine Johnson"),
            ],
        };

    public static Contact CreateContact() =>
        new()
        {
            OdataType = "#microsoft.graph.contact",
            Id = "contact-grace-hopper",
            DisplayName = "Grace Hopper",
            GivenName = "Grace",
            Surname = "Hopper",
            CompanyName = "Contoso",
            JobTitle = "Principal Engineer",
            EmailAddresses =
            [
                new EmailAddress
                {
                    Name = "Grace Hopper",
                    Address = "grace.hopper@contoso.example",
                },
                new EmailAddress
                {
                    Name = "Grace Hopper (Alt)",
                    Address = "grace.hopper@fabrikam.example",
                },
            ],
            BusinessPhones =
            [
                "+1 206 555 0110",
            ],
            HomePhones =
            [
                "+1 206 555 0199",
            ],
            Categories =
            [
                "engineering",
                "leadership",
            ],
            BusinessAddress = CreateAddress("500 Business Rd", "Seattle", "USA", "98109"),
            HomeAddress = CreateAddress("42 Compiler Ln", "Arlington", "USA", "22201"),
            OtherAddress = CreateAddress("1 Research Park", "Boston", "USA", "02110"),
            Children =
            [
                "Sam Hopper",
                "Amy Hopper",
            ],
        };

    public static DriveItem CreateDriveItem() =>
        new()
        {
            OdataType = "#microsoft.graph.driveItem",
            Id = "driveitem-root-architecture",
            Name = "Architecture",
            Description = CreateLongText("Shared architecture documents and benchmark notes.", 4),
            ParentReference = new ItemReference
            {
                DriveId = "drive-1",
                Id = "root",
                Name = "Documents",
                Path = "/drive/root:/Documents",
            },
            Folder = new Folder
            {
                ChildCount = 2,
            },
            CreatedBy = CreateIdentitySet("Ada Lovelace", "user-ada-lovelace"),
            LastModifiedBy = CreateIdentitySet("Grace Hopper", "user-grace-hopper"),
            Children =
            [
                new DriveItem
                {
                    OdataType = "#microsoft.graph.driveItem",
                    Id = "driveitem-child-report",
                    Name = "serialization-report.md",
                    ParentReference = new ItemReference
                    {
                        DriveId = "drive-1",
                        Id = "driveitem-root-architecture",
                        Name = "Architecture",
                        Path = "/drive/root:/Documents/Architecture",
                    },
                    File = new FileObject
                    {
                        MimeType = "text/markdown",
                    },
                    CreatedBy = CreateIdentitySet("Ada Lovelace", "user-ada-lovelace"),
                    LastModifiedBy = CreateIdentitySet("Margaret Hamilton", "user-margaret-hamilton"),
                },
                new DriveItem
                {
                    OdataType = "#microsoft.graph.driveItem",
                    Id = "driveitem-child-benchmarks",
                    Name = "Benchmarks",
                    ParentReference = new ItemReference
                    {
                        DriveId = "drive-1",
                        Id = "driveitem-root-architecture",
                        Name = "Architecture",
                        Path = "/drive/root:/Documents/Architecture",
                    },
                    Folder = new Folder
                    {
                        ChildCount = 1,
                    },
                    Children =
                    [
                        new DriveItem
                        {
                            OdataType = "#microsoft.graph.driveItem",
                            Id = "driveitem-grandchild-csv",
                            Name = "results.csv",
                            ParentReference = new ItemReference
                            {
                                DriveId = "drive-1",
                                Id = "driveitem-child-benchmarks",
                                Name = "Benchmarks",
                                Path = "/drive/root:/Documents/Architecture/Benchmarks",
                            },
                            File = new FileObject
                            {
                                MimeType = "text/csv",
                            },
                            CreatedBy = CreateIdentitySet("Donald Knuth", "user-donald-knuth"),
                            LastModifiedBy = CreateIdentitySet("Grace Hopper", "user-grace-hopper"),
                        },
                    ],
                },
            ],
        };

    public static Team CreateTeam() =>
        new()
        {
            OdataType = "#microsoft.graph.team",
            Id = "team-platform-serialization",
            DisplayName = "Platform Serialization Team",
            Description = CreateLongText("Coordinates Kiota serialization work, test coverage, and benchmark analysis.", 5),
            Classification = "Engineering",
            FunSettings = new TeamFunSettings
            {
                AllowCustomMemes = true,
                AllowGiphy = false,
                AllowStickersAndMemes = true,
            },
            MemberSettings = new TeamMemberSettings
            {
                AllowAddRemoveApps = true,
                AllowCreatePrivateChannels = true,
                AllowCreateUpdateChannels = true,
            },
            MessagingSettings = new TeamMessagingSettings
            {
                AllowChannelMentions = true,
                AllowTeamMentions = true,
                AllowUserEditMessages = true,
                AllowUserDeleteMessages = false,
            },
            Summary = new TeamSummary
            {
                MembersCount = 8,
                OwnersCount = 2,
                GuestsCount = 1,
            },
            Group = CreateGroup(),
        };

    public static Chat CreateChat() =>
        new()
        {
            OdataType = "#microsoft.graph.chat",
            Id = "chat-orleans-kiota",
            Topic = "Orleans + Kiota serializer rollout",
            TenantId = "tenant-contoso",
            WebUrl = "https://teams.microsoft.example/l/chat-orleans-kiota",
            IsHiddenForAllMembers = false,
            CreatedDateTime = DateTimeOffset.Parse("2026-03-15T14:00:00Z"),
            LastUpdatedDateTime = DateTimeOffset.Parse("2026-03-16T08:45:00Z"),
            LastMessagePreview = new ChatMessageInfo
            {
                OdataType = "#microsoft.graph.chatMessageInfo",
                Id = "chatmessage-preview-release-plan",
                CreatedDateTime = DateTimeOffset.Parse("2026-03-16T08:40:00Z"),
                IsDeleted = false,
                Body = new ItemBody
                {
                    Content = CreateLongText("Previewing the latest serializer coverage status for chat entities.", 4),
                },
                From = new ChatMessageFromIdentitySet
                {
                    User = new Identity
                    {
                        DisplayName = "Ada Lovelace",
                        Id = "user-ada-lovelace",
                    },
                },
            },
            Viewpoint = new ChatViewpoint
            {
                OdataType = "#microsoft.graph.chatViewpoint",
                IsHidden = false,
                LastMessageReadDateTime = DateTimeOffset.Parse("2026-03-16T08:41:00Z"),
            },
            Members =
            [
                new AadUserConversationMember
                {
                    OdataType = "#microsoft.graph.aadUserConversationMember",
                    Id = "member-ada-lovelace",
                    DisplayName = "Ada Lovelace",
                    Email = "ada.lovelace@contoso.example",
                    Roles =
                    [
                        "owner",
                    ],
                    TenantId = "tenant-contoso",
                    UserId = "user-ada-lovelace",
                    VisibleHistoryStartDateTime = DateTimeOffset.Parse("2026-03-15T14:00:00Z"),
                    User = new User
                    {
                        OdataType = "#microsoft.graph.user",
                        Id = "user-ada-lovelace",
                        DisplayName = "Ada Lovelace",
                        Mail = "ada.lovelace@contoso.example",
                    },
                },
                new AadUserConversationMember
                {
                    OdataType = "#microsoft.graph.aadUserConversationMember",
                    Id = "member-grace-hopper",
                    DisplayName = "Grace Hopper",
                    Email = "grace.hopper@contoso.example",
                    Roles =
                    [
                        "member",
                    ],
                    TenantId = "tenant-contoso",
                    UserId = "user-grace-hopper",
                    VisibleHistoryStartDateTime = DateTimeOffset.Parse("2026-03-15T14:05:00Z"),
                    User = new User
                    {
                        OdataType = "#microsoft.graph.user",
                        Id = "user-grace-hopper",
                        DisplayName = "Grace Hopper",
                        Mail = "grace.hopper@contoso.example",
                    },
                },
            ],
            Messages =
            [
                CreateChatMessage(),
            ],
        };

    public static ChatMessage CreateChatMessage() =>
        new()
        {
            OdataType = "#microsoft.graph.chatMessage",
            Id = "chatmessage-release-plan",
            ChatId = "chat-orleans-kiota",
            Subject = "Release readiness",
            Summary = "Review serializer coverage for chat entities.",
            Locale = "en-US",
            WebUrl = "https://teams.microsoft.example/l/message/chatmessage-release-plan",
            ReplyToId = "chatmessage-root",
            Etag = "chatmessage-v1",
            CreatedDateTime = DateTimeOffset.Parse("2026-03-16T08:40:00Z"),
            LastEditedDateTime = DateTimeOffset.Parse("2026-03-16T08:42:00Z"),
            LastModifiedDateTime = DateTimeOffset.Parse("2026-03-16T08:43:00Z"),
            Body = new ItemBody
            {
                Content = CreateLongText("ChatMessage payloads should preserve collections, hosted content, and replies.", 6),
            },
            From = new ChatMessageFromIdentitySet
            {
                User = new Identity
                {
                    DisplayName = "Ada Lovelace",
                    Id = "user-ada-lovelace",
                },
            },
            Attachments =
            [
                new ChatMessageAttachment
                {
                    Id = "attachment-release-checklist",
                    Content = "release-checklist.json",
                    ContentType = "reference",
                    ContentUrl = "https://contoso.example/release-checklist.json",
                    Name = "Release checklist",
                    TeamsAppId = "teams-app-kiota",
                    ThumbnailUrl = "https://contoso.example/release-checklist.png",
                },
            ],
            HostedContents =
            [
                new ChatMessageHostedContent
                {
                    Id = "hosted-content-inline-note",
                    ContentType = "text/plain",
                    ContentBytes = Encoding.UTF8.GetBytes(CreateLongText("Hosted content bytes for inline chat rendering.", 4)),
                },
            ],
            Mentions =
            [
                new ChatMessageMention
                {
                    Id = 0,
                    MentionText = "Grace Hopper",
                    Mentioned = new ChatMessageMentionedIdentitySet
                    {
                        User = new Identity
                        {
                            DisplayName = "Grace Hopper",
                            Id = "user-grace-hopper",
                        },
                    },
                },
            ],
            MessageHistory =
            [
                new ChatMessageHistoryItem
                {
                    ModifiedDateTime = DateTimeOffset.Parse("2026-03-16T08:42:30Z"),
                    Reaction = new ChatMessageReaction
                    {
                        CreatedDateTime = DateTimeOffset.Parse("2026-03-16T08:42:15Z"),
                        DisplayName = "Grace Hopper",
                        ReactionContentUrl = "https://contoso.example/reactions/like",
                        ReactionType = "like",
                        User = new ChatMessageReactionIdentitySet
                        {
                            User = new Identity
                            {
                                DisplayName = "Grace Hopper",
                                Id = "user-grace-hopper",
                            },
                        },
                    },
                },
            ],
            Reactions =
            [
                new ChatMessageReaction
                {
                    CreatedDateTime = DateTimeOffset.Parse("2026-03-16T08:42:15Z"),
                    DisplayName = "Grace Hopper",
                    ReactionContentUrl = "https://contoso.example/reactions/like",
                    ReactionType = "like",
                    User = new ChatMessageReactionIdentitySet
                    {
                        User = new Identity
                        {
                            DisplayName = "Grace Hopper",
                            Id = "user-grace-hopper",
                        },
                    },
                },
            ],
            Replies =
            [
                new ChatMessage
                {
                    OdataType = "#microsoft.graph.chatMessage",
                    Id = "chatmessage-release-plan-reply",
                    ChatId = "chat-orleans-kiota",
                    ReplyToId = "chatmessage-release-plan",
                    Summary = "Follow-up approval.",
                    Locale = "en-US",
                    WebUrl = "https://teams.microsoft.example/l/message/chatmessage-release-plan-reply",
                    CreatedDateTime = DateTimeOffset.Parse("2026-03-16T08:44:00Z"),
                    Body = new ItemBody
                    {
                        Content = "Ship it.",
                    },
                    From = new ChatMessageFromIdentitySet
                    {
                        User = new Identity
                        {
                            DisplayName = "Grace Hopper",
                            Id = "user-grace-hopper",
                        },
                    },
                },
            ],
        };

    private static Recipient CreateRecipient(string name, string address) =>
        new()
        {
            EmailAddress = new EmailAddress
            {
                Name = name,
                Address = address,
            },
        };

    private static User CreateMemberUser(string id, string displayName) =>
        new()
        {
            OdataType = "#microsoft.graph.user",
            Id = id,
            DisplayName = displayName,
            Mail = $"{id}@contoso.example",
        };

    private static PhysicalAddress CreateAddress(string street, string city, string countryOrRegion, string postalCode) =>
        new()
        {
            Street = street,
            City = city,
            CountryOrRegion = countryOrRegion,
            PostalCode = postalCode,
        };

    private static IdentitySet CreateIdentitySet(string displayName, string id) =>
        new()
        {
            User = new Identity
            {
                DisplayName = displayName,
                Id = id,
            },
        };

    private static string CreateLongText(string sentence, int repetitions) =>
        string.Join(' ', Enumerable.Repeat(sentence, repetitions));
}
