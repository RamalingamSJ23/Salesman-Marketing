using MarketingApi.Common;
using MarketingApi.Controllers;
using MarketingApi.Models.Entities;
using MarketingApi.Repository.Interfaces;
using MarketingApi.Service.Implementations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace MarketingApi.Tests;

public class UsersAndSecurityTests
{
    [Fact]
    public async Task Authenticate_DoesNotTrimPassword_C3()
    {
        var user = new User { Id = 1, Username = "sup", Role = new Role { RoleName = "Supervisor Master" } };
        var hasher = new PasswordHasher<User>();
        user.PasswordHash = hasher.HashPassword(user, "Secret@123");

        var repoMock = new Mock<IUsersRepository>();
        repoMock.Setup(r => r.GetUserByUsernameAsync("sup", true)).ReturnsAsync(user);
        var service = new UsersService(repoMock.Object, NullLogger<UsersService>.Instance);

        var paddedResult = await service.AuthenticateAsync("sup", " Secret@123 ");
        var exactResult = await service.AuthenticateAsync("sup", "Secret@123");

        Assert.Null(paddedResult);
        Assert.NotNull(exactResult);
    }

    [Fact]
    public async Task Authenticate_LegacyPlaintextPassword_RehashesAndSaves_C3()
    {
        var user = new User
        {
            Id = 2,
            Username = "mkt",
            PasswordHash = "Plain@123",
            Role = new Role { RoleName = "Marketing Master" }
        };

        var repoMock = new Mock<IUsersRepository>();
        repoMock.Setup(r => r.GetUserByUsernameAsync("mkt", true)).ReturnsAsync(user);
        var service = new UsersService(repoMock.Object, NullLogger<UsersService>.Instance);

        var authenticated = await service.AuthenticateAsync("mkt", "Plain@123");

        Assert.NotNull(authenticated);
        Assert.StartsWith("AQAAAA", user.PasswordHash);
        repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public void AppRoles_Normalize_MapsLegacySpelling()
    {
        Assert.Equal(AppRoles.SupervisorMaster, AppRoles.Normalize("Superviser Master"));
        Assert.Equal(AppRoles.MarketingMaster, AppRoles.Normalize("Marketing Master"));
    }

    [Fact]
    public void Controllers_EnforceExpectedRolePolicies_C2()
    {
        var approveMethod = typeof(SalesmanMasterController).GetMethod(nameof(SalesmanMasterController.ApproveDeactivation))!;
        var approveAttr = approveMethod.GetCustomAttributes(typeof(AuthorizeAttribute), false).Cast<AuthorizeAttribute>().Single();
        Assert.Equal(AppRoles.MarketingPolicy, approveAttr.Policy);

        var createSalesmanMethod = typeof(SalesmanMasterController).GetMethod(nameof(SalesmanMasterController.CreateSalesman))!;
        var createAttr = createSalesmanMethod.GetCustomAttributes(typeof(AuthorizeAttribute), false).Cast<AuthorizeAttribute>().Single();
        Assert.Equal(AppRoles.SalesmanPolicy, createAttr.Policy);

        var teamControllerAttr = typeof(MarketingTeamMasterController).GetCustomAttributes(typeof(AuthorizeAttribute), false).Cast<AuthorizeAttribute>().Single();
        Assert.Equal(AppRoles.MarketingPolicy, teamControllerAttr.Policy);
    }
}
