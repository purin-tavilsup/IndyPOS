using FluentAssertions;
using IndyPOS.Domain.Entities.Core;
using Xunit;

namespace IndyPOS.Domain.Tests.Entities;

public class CashDrawerEntryTests
{
    private static readonly Guid CreatorId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid EditorId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateOnly BusinessDate = new(2026, 9, 26);
    private static readonly DateTime CreatedUtc = new(2026, 9, 26, 1, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime LaterUtc = new(2026, 9, 26, 5, 0, 0, DateTimeKind.Utc);

    private static CashFloat NewEntry() => new()
    {
        Id = Guid.NewGuid(),
        StoreId = "test-store",
        Amount = 500m,
        BusinessDate = BusinessDate,
        CreatedUtc = CreatedUtc,
        LastModifiedUtc = CreatedUtc,
        CreatedByUserId = CreatorId
    };

    [Fact]
    public void MarkDeleted_WhenAlreadyDeleted_Throws()
    {
        var entry = NewEntry();
        entry.MarkDeleted(EditorId, LaterUtc);

        var act = () => entry.MarkDeleted(CreatorId, LaterUtc.AddHours(1));

        act.Should()
           .Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkDeleted_WhenAlreadyDeleted_KeepsOriginalAuditFields()
    {
        var entry = NewEntry();
        entry.MarkDeleted(EditorId, LaterUtc);

        try { entry.MarkDeleted(CreatorId, LaterUtc.AddHours(1)); } catch (InvalidOperationException) { }

        entry.DeletedUtc.Should()
                        .Be(LaterUtc);
        entry.LastModifiedByUserId.Should()
                                  .Be(EditorId);
    }

    [Fact]
    public void Touch_WithLaterTime_DoesNotChangeBusinessDate()
    {
        var entry = NewEntry();

        entry.Touch(EditorId, LaterUtc.AddDays(1));

        entry.BusinessDate.Should()
                          .Be(BusinessDate);
    }

    [Fact]
    public void MarkDeleted_WhenActive_SetsDeletedFlag()
    {
        var entry = NewEntry();

        entry.MarkDeleted(EditorId, LaterUtc);

        entry.IsDeleted.Should()
                       .BeTrue();
    }

    [Fact]
    public void MarkDeleted_WhenActive_SetsDeletedTime()
    {
        var entry = NewEntry();

        entry.MarkDeleted(EditorId, LaterUtc);

        entry.DeletedUtc.Should()
                        .Be(LaterUtc);
    }

    [Fact]
    public void MarkDeleted_WhenActive_RecordsWhoDeleted()
    {
        var entry = NewEntry();

        entry.MarkDeleted(EditorId, LaterUtc);

        entry.LastModifiedByUserId.Should()
                                  .Be(EditorId);
    }

    [Fact]
    public void MarkDeleted_WhenActive_RecordsWhenDeleted()
    {
        var entry = NewEntry();

        entry.MarkDeleted(EditorId, LaterUtc);

        entry.LastModifiedUtc.Should()
                             .Be(LaterUtc);
    }

    [Fact]
    public void Touch_WithEditor_RecordsTheEditor()
    {
        var entry = NewEntry();

        entry.Touch(EditorId, LaterUtc);

        entry.LastModifiedByUserId.Should()
                                  .Be(EditorId);
    }

    [Fact]
    public void Touch_WithEditor_RecordsTheTime()
    {
        var entry = NewEntry();

        entry.Touch(EditorId, LaterUtc);

        entry.LastModifiedUtc.Should()
                             .Be(LaterUtc);
    }
}
