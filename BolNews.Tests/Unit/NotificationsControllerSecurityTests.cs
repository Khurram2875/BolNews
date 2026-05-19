using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;
using BolNews.Web.Areas.Admin.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace BolNews.Tests.Unit;

public class NotificationsControllerSecurityTests
{
    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    [Fact]
    public async Task Open_OwnerCanMarkRead_AndLocalRedirectWorks()
    {
        var user = new ApplicationUser { Id = "user-1", UserName = "u1" };
        var notificationService = new Mock<INotificationService>();
        notificationService
            .Setup(x => x.MarkReadAsync(10, "user-1"))
            .ReturnsAsync(true);

        var userManager = CreateUserManagerMock();
        userManager.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(user);

        var controller = new NotificationsController(notificationService.Object, userManager.Object)
        {
            Url = Mock.Of<IUrlHelper>(x => x.IsLocalUrl("/Admin/Notifications") == true)
        };

        var result = await controller.Open(10, "/Admin/Notifications");

        result.Should().BeOfType<LocalRedirectResult>()
            .Which.Url.Should().Be("/Admin/Notifications");
        notificationService.Verify(x => x.MarkReadAsync(10, "user-1"), Times.Once);
    }

    [Fact]
    public async Task Open_ExternalRedirectAttempt_Blocked()
    {
        var user = new ApplicationUser { Id = "user-1", UserName = "u1" };
        var notificationService = new Mock<INotificationService>();
        notificationService.Setup(x => x.MarkReadAsync(10, "user-1")).ReturnsAsync(true);

        var userManager = CreateUserManagerMock();
        userManager.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(user);

        var urlHelper = new Mock<IUrlHelper>();
        urlHelper.Setup(x => x.IsLocalUrl("https://evil.example.com")).Returns(false);

        var controller = new NotificationsController(notificationService.Object, userManager.Object)
        {
            Url = urlHelper.Object
        };

        var result = await controller.Open(10, "https://evil.example.com");

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Index");
    }

    [Fact]
    public async Task Open_CallsMarkReadWithCurrentUserId_PreventingCrossUserMark()
    {
        var user = new ApplicationUser { Id = "owner-user", UserName = "owner" };
        var notificationService = new Mock<INotificationService>();
        notificationService.Setup(x => x.MarkReadAsync(99, "owner-user")).ReturnsAsync(false);

        var userManager = CreateUserManagerMock();
        userManager.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(user);

        var controller = new NotificationsController(notificationService.Object, userManager.Object)
        {
            Url = Mock.Of<IUrlHelper>(x => x.IsLocalUrl((string?)null!) == false)
        };

        await controller.Open(99, null);

        notificationService.Verify(x => x.MarkReadAsync(99, "owner-user"), Times.Once);
        notificationService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Feed_ReturnsUnreadCountAndItems_ForCurrentUser()
    {
        var user = new ApplicationUser { Id = "user-1", UserName = "u1" };
        var notificationService = new Mock<INotificationService>();
        notificationService.Setup(x => x.GetUnreadAsync("user-1"))
            .ReturnsAsync(new List<Notification>
            {
                new() { Id = 1, Title = "A", Message = "M1", Url = "/Admin/Articles/Edit/1", CreatedAt = DateTime.UtcNow },
                new() { Id = 2, Title = "B", Message = "M2", Url = "/Admin/Articles/Edit/2", CreatedAt = DateTime.UtcNow.AddMinutes(-1) }
            });

        var userManager = CreateUserManagerMock();
        userManager.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(user);

        var controller = new NotificationsController(notificationService.Object, userManager.Object);
        var result = await controller.Feed();

        result.Should().BeOfType<JsonResult>();
        notificationService.Verify(x => x.GetUnreadAsync("user-1"), Times.Once);
    }

    [Fact]
    public async Task Feed_WhenUserMissing_ReturnsChallenge()
    {
        var notificationService = new Mock<INotificationService>();
        var userManager = CreateUserManagerMock();
        userManager.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync((ApplicationUser?)null);

        var controller = new NotificationsController(notificationService.Object, userManager.Object);
        var result = await controller.Feed();

        result.Should().BeOfType<ChallengeResult>();
    }
}
