using BolNews.Domain.Entities;
using BolNews.Persistence.Repositories;
using BolNews.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace BolNews.Tests.Integration;

public class NotificationRepositorySecurityTests
{
    [Fact]
    public async Task MarkReadAsync_Owner_CanMarkOwnNotification()
    {
        var context = TestDbContextFactory.CreateWithSeed();
        context.Notifications.Add(new Notification
        {
            Id = 1,
            UserId = "user-author-1",
            Title = "T1",
            Message = "M1",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var repo = new NotificationRepository(context);
        var marked = await repo.MarkReadAsync(1, "user-author-1");

        marked.Should().BeTrue();
        context.Notifications.Single(x => x.Id == 1).IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task MarkReadAsync_NonOwner_CannotMarkAnotherUsersNotification()
    {
        var context = TestDbContextFactory.CreateWithSeed();
        context.Notifications.Add(new Notification
        {
            Id = 2,
            UserId = "user-author-1",
            Title = "T2",
            Message = "M2",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var repo = new NotificationRepository(context);
        var marked = await repo.MarkReadAsync(2, "user-author-2");

        marked.Should().BeFalse();
        context.Notifications.Single(x => x.Id == 2).IsRead.Should().BeFalse();
    }
}
