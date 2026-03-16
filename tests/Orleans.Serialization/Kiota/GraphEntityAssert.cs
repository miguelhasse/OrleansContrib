using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions.Serialization;

namespace Orleans.Serialization.Kiota.Tests;

internal static class GraphEntityAssert
{
    public static void Equal(IParsable expected, IParsable actual)
    {
        Assert.True(
            AreEqual(expected, actual),
            $"Expected {expected.GetType().Name} and actual {actual.GetType().Name} to contain the same graph payload.");
    }

    public static bool AreEqual(IParsable? expected, IParsable? actual) =>
        (expected, actual) switch
        {
            (null, null) => true,
            (User left, User right) => AreEqual(left, right),
            (Message left, Message right) => AreEqual(left, right),
            (Event left, Event right) => AreEqual(left, right),
            (Group left, Group right) => AreEqual(left, right),
            (Contact left, Contact right) => AreEqual(left, right),
            (DriveItem left, DriveItem right) => AreEqual(left, right),
            (Team left, Team right) => AreEqual(left, right),
            _ => false,
        };

    public static bool AreEqual(Message? expected, Message? actual)
    {
        if (expected is null || actual is null)
            return expected is null && actual is null;

        return expected.OdataType == actual.OdataType
            && expected.Id == actual.Id
            && expected.Subject == actual.Subject
            && AreEqual(expected.Body, actual.Body)
            && AreEqual(expected.From, actual.From)
            && SequenceEqual(expected.ToRecipients, actual.ToRecipients, AreEqual)
            && SequenceEqual(expected.ReplyTo, actual.ReplyTo, AreEqual)
            && SequenceEqual(expected.InternetMessageHeaders, actual.InternetMessageHeaders, AreEqual)
            && SequenceEqual(expected.Attachments, actual.Attachments, AreEqual);
    }

    public static bool AreEqual(User? expected, User? actual)
    {
        if (expected is null || actual is null)
            return expected is null && actual is null;

        return expected.OdataType == actual.OdataType
            && expected.Id == actual.Id
            && expected.DisplayName == actual.DisplayName
            && expected.GivenName == actual.GivenName
            && expected.Surname == actual.Surname
            && expected.Mail == actual.Mail
            && SequenceEqual(expected.BusinessPhones, actual.BusinessPhones, static (left, right) => left == right)
            && SequenceEqual(expected.Identities, actual.Identities, AreEqual)
            && AreEqual(expected.EmployeeOrgData, actual.EmployeeOrgData)
            && AreEqual(expected.Manager, actual.Manager);
    }

    public static bool AreEqual(Event? expected, Event? actual)
    {
        if (expected is null || actual is null)
            return expected is null && actual is null;

        return expected.OdataType == actual.OdataType
            && expected.Id == actual.Id
            && expected.Subject == actual.Subject
            && AreEqual(expected.Body, actual.Body)
            && AreEqual(expected.Start, actual.Start)
            && AreEqual(expected.End, actual.End)
            && AreEqual(expected.Organizer, actual.Organizer)
            && SequenceEqual(expected.Attendees, actual.Attendees, AreEqual)
            && AreEqual(expected.Location, actual.Location)
            && SequenceEqual(expected.Locations, actual.Locations, AreEqual);
    }

    public static bool AreEqual(Group? expected, Group? actual)
    {
        if (expected is null || actual is null)
            return expected is null && actual is null;

        return expected.OdataType == actual.OdataType
            && expected.Id == actual.Id
            && expected.DisplayName == actual.DisplayName
            && expected.Description == actual.Description
            && expected.MailNickname == actual.MailNickname
            && expected.MailEnabled == actual.MailEnabled
            && expected.SecurityEnabled == actual.SecurityEnabled
            && SequenceEqual(expected.GroupTypes, actual.GroupTypes, static (left, right) => left == right)
            && SequenceEqual(expected.Members, actual.Members, AreEqual)
            && SequenceEqual(expected.Owners, actual.Owners, AreEqual);
    }

    public static bool AreEqual(Contact? expected, Contact? actual)
    {
        if (expected is null || actual is null)
            return expected is null && actual is null;

        return expected.OdataType == actual.OdataType
            && expected.Id == actual.Id
            && expected.DisplayName == actual.DisplayName
            && expected.GivenName == actual.GivenName
            && expected.Surname == actual.Surname
            && expected.CompanyName == actual.CompanyName
            && expected.JobTitle == actual.JobTitle
            && SequenceEqual(expected.EmailAddresses, actual.EmailAddresses, AreEqual)
            && SequenceEqual(expected.BusinessPhones, actual.BusinessPhones, static (left, right) => left == right)
            && SequenceEqual(expected.HomePhones, actual.HomePhones, static (left, right) => left == right)
            && SequenceEqual(expected.Categories, actual.Categories, static (left, right) => left == right)
            && AreEqual(expected.BusinessAddress, actual.BusinessAddress)
            && AreEqual(expected.HomeAddress, actual.HomeAddress)
            && AreEqual(expected.OtherAddress, actual.OtherAddress)
            && SequenceEqual(expected.Children, actual.Children, static (left, right) => left == right);
    }

    public static bool AreEqual(DriveItem? expected, DriveItem? actual)
    {
        if (expected is null || actual is null)
            return expected is null && actual is null;

        return expected.OdataType == actual.OdataType
            && expected.Id == actual.Id
            && expected.Name == actual.Name
            && expected.Description == actual.Description
            && AreEqual(expected.ParentReference, actual.ParentReference)
            && AreEqual(expected.Folder, actual.Folder)
            && AreEqual(expected.File, actual.File)
            && AreEqual(expected.CreatedBy, actual.CreatedBy)
            && AreEqual(expected.LastModifiedBy, actual.LastModifiedBy)
            && SequenceEqual(expected.Children, actual.Children, AreEqual);
    }

    public static bool AreEqual(Team? expected, Team? actual)
    {
        if (expected is null || actual is null)
            return expected is null && actual is null;

        return expected.OdataType == actual.OdataType
            && expected.Id == actual.Id
            && expected.DisplayName == actual.DisplayName
            && expected.Description == actual.Description
            && expected.Classification == actual.Classification
            && AreEqual(expected.FunSettings, actual.FunSettings)
            && AreEqual(expected.MemberSettings, actual.MemberSettings)
            && AreEqual(expected.MessagingSettings, actual.MessagingSettings)
            && AreEqual(expected.Summary, actual.Summary)
            && AreEqual(expected.Group, actual.Group);
    }

    private static bool AreEqual(ItemBody? expected, ItemBody? actual) =>
        expected?.Content == actual?.Content;

    private static bool AreEqual(Recipient? expected, Recipient? actual) =>
        AreEqual(expected?.EmailAddress, actual?.EmailAddress);

    private static bool AreEqual(EmailAddress? expected, EmailAddress? actual) =>
        expected?.Name == actual?.Name
        && expected?.Address == actual?.Address;

    private static bool AreEqual(DateTimeTimeZone? expected, DateTimeTimeZone? actual) =>
        expected?.DateTime == actual?.DateTime
        && expected?.TimeZone == actual?.TimeZone;

    private static bool AreEqual(Location? expected, Location? actual) =>
        expected?.DisplayName == actual?.DisplayName
        && AreEqual(expected?.Address, actual?.Address);

    private static bool AreEqual(PhysicalAddress? expected, PhysicalAddress? actual) =>
        expected?.Street == actual?.Street
        && expected?.City == actual?.City
        && expected?.CountryOrRegion == actual?.CountryOrRegion
        && expected?.PostalCode == actual?.PostalCode;

    private static bool AreEqual(Attendee? expected, Attendee? actual) =>
        AreEqual(expected?.EmailAddress, actual?.EmailAddress);

    private static bool AreEqual(EmployeeOrgData? expected, EmployeeOrgData? actual) =>
        expected?.Division == actual?.Division
        && expected?.CostCenter == actual?.CostCenter;

    private static bool AreEqual(Identity? expected, Identity? actual) =>
        expected?.DisplayName == actual?.DisplayName
        && expected?.Id == actual?.Id;

    private static bool AreEqual(IdentitySet? expected, IdentitySet? actual) =>
        AreEqual(expected?.User, actual?.User)
        && AreEqual(expected?.Application, actual?.Application)
        && AreEqual(expected?.Device, actual?.Device);

    private static bool AreEqual(ObjectIdentity? expected, ObjectIdentity? actual) =>
        expected?.SignInType == actual?.SignInType
        && expected?.Issuer == actual?.Issuer
        && expected?.IssuerAssignedId == actual?.IssuerAssignedId;

    private static bool AreEqual(InternetMessageHeader? expected, InternetMessageHeader? actual) =>
        expected?.Name == actual?.Name
        && expected?.Value == actual?.Value;

    private static bool AreEqual(Attachment? expected, Attachment? actual)
    {
        if (expected is null || actual is null)
            return expected is null && actual is null;

        return (expected, actual) switch
        {
            (FileAttachment left, FileAttachment right) => left.OdataType == right.OdataType
                && left.Name == right.Name
                && left.ContentType == right.ContentType
                && (left.ContentBytes ?? []).SequenceEqual(right.ContentBytes ?? []),
            _ => expected.Id == actual.Id,
        };
    }

    private static bool AreEqual(ItemReference? expected, ItemReference? actual) =>
        expected?.DriveId == actual?.DriveId
        && expected?.Id == actual?.Id
        && expected?.Name == actual?.Name
        && expected?.Path == actual?.Path;

    private static bool AreEqual(Folder? expected, Folder? actual) =>
        expected?.ChildCount == actual?.ChildCount;

    private static bool AreEqual(FileObject? expected, FileObject? actual) =>
        expected?.MimeType == actual?.MimeType;

    private static bool AreEqual(TeamFunSettings? expected, TeamFunSettings? actual) =>
        expected?.AllowCustomMemes == actual?.AllowCustomMemes
        && expected?.AllowGiphy == actual?.AllowGiphy
        && expected?.AllowStickersAndMemes == actual?.AllowStickersAndMemes;

    private static bool AreEqual(TeamMemberSettings? expected, TeamMemberSettings? actual) =>
        expected?.AllowAddRemoveApps == actual?.AllowAddRemoveApps
        && expected?.AllowCreatePrivateChannels == actual?.AllowCreatePrivateChannels
        && expected?.AllowCreateUpdateChannels == actual?.AllowCreateUpdateChannels;

    private static bool AreEqual(TeamMessagingSettings? expected, TeamMessagingSettings? actual) =>
        expected?.AllowChannelMentions == actual?.AllowChannelMentions
        && expected?.AllowTeamMentions == actual?.AllowTeamMentions
        && expected?.AllowUserEditMessages == actual?.AllowUserEditMessages
        && expected?.AllowUserDeleteMessages == actual?.AllowUserDeleteMessages;

    private static bool AreEqual(TeamSummary? expected, TeamSummary? actual) =>
        expected?.GuestsCount == actual?.GuestsCount
        && expected?.MembersCount == actual?.MembersCount
        && expected?.OwnersCount == actual?.OwnersCount;

    private static bool AreEqual(DirectoryObject? expected, DirectoryObject? actual) =>
        (expected, actual) switch
        {
            (null, null) => true,
            (User left, User right) => left.OdataType == right.OdataType
                && left.Id == right.Id
                && left.DisplayName == right.DisplayName
                && left.Mail == right.Mail,
            (Group left, Group right) => AreEqual(left, right),
            _ => false,
        };

    private static bool SequenceEqual<T>(
        IReadOnlyList<T>? expected,
        IReadOnlyList<T>? actual,
        Func<T?, T?, bool> comparer) =>
        (expected, actual) switch
        {
            (null, null) => true,
            (null, not null) => false,
            (not null, null) => false,
            _ => expected!.Count == actual!.Count && expected.Zip(actual, comparer).All(result => result),
        };
}
