using BolNews.Domain.Entities;
using BolNews.Persistence.Context;
using BolNews.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BolNews.Tests.Unit;

public class UserAdminServicePerformanceTests
{
    [Fact]
    public async Task GetUsersWithRolesAsync_ShouldNotCallUserManagerGetRolesPerUser()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);

        var user1 = new ApplicationUser { Id = "u1", UserName = "u1@test.com", Email = "u1@test.com", FullName = "U1" };
        var user2 = new ApplicationUser { Id = "u2", UserName = "u2@test.com", Email = "u2@test.com", FullName = "U2" };
        var role = new IdentityRole { Id = "r1", Name = "Admin", NormalizedName = "ADMIN" };

        context.Users.AddRange(user1, user2);
        context.Roles.Add(role);
        context.UserRoles.AddRange(
            new IdentityUserRole<string> { UserId = "u1", RoleId = "r1" },
            new IdentityUserRole<string> { UserId = "u2", RoleId = "r1" }
        );
        await context.SaveChangesAsync();

        var store = new UserStore<ApplicationUser>(context);
        var userManagerMock = new Mock<UserManager<ApplicationUser>>(
            store, null!, null!, null!, null!, null!, null!, null!, null!);
        userManagerMock.Setup(x => x.Users).Returns(context.Users);
        userManagerMock
            .Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .Throws(new Exception("N+1 path should not be used"));

        var roleStore = new RoleStore<IdentityRole>(context);
        var roleManager = new RoleManager<IdentityRole>(roleStore, null!, null!, null!, null!);

        var service = new UserAdminService(userManagerMock.Object, roleManager, context);

        var result = await service.GetUsersWithRolesAsync();

        Assert.Equal(2, result.Count);
        userManagerMock.Verify(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }
}
