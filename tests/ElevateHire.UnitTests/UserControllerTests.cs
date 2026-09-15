using ElevateWorkforce.Application.DTOs;
using ElevateWorkforce.Application.Interfaces;
using ElevateWorkforce.Domain.Entities;
using ElevateWorkforce.Web.Controllers;
using ElevateWorkforce.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace ElevateWorkforce.UnitTests;

public class UserControllerTests
{
    [Fact]
    public async Task Register_RejectsAdminRole()
    {
        var userManager = CreateUserManager();
        var controller = CreateController(userManager.Object);

        var result = await controller.Register(new RegisterUserDto
        {
            FullName = "Admin Attempt",
            Email = "admin@example.com",
            Password = "Password1",
            Role = "Admin"
        }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        userManager.Verify(manager => manager.FindByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Register_PassesPasswordToIdentity_AndReturnsCreatedResponse()
    {
        var userManager = CreateUserManager();
        var roleManager = CreateRoleManager();
        var userService = new Mock<IUserService>();
        var cache = new Mock<ICacheService>();
        var email = new Mock<IEmailService>();
        userManager.Setup(manager => manager.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);
        userManager.Setup(manager => manager.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .Callback<User, string>((user, _) => user.Id = 42)
            .ReturnsAsync(IdentityResult.Success);
        roleManager.Setup(manager => manager.RoleExistsAsync("JobSeeker")).ReturnsAsync(true);
        userManager.Setup(manager => manager.AddToRoleAsync(It.IsAny<User>(), "JobSeeker"))
            .ReturnsAsync(IdentityResult.Success);
        userService.Setup(service => service.EnsureJobSeekerAsync(It.IsAny<User>()))
            .ReturnsAsync(new JobSeeker());

        var controller = CreateController(userManager.Object, roleManager.Object, userService.Object, cache.Object, email.Object);

        var result = await controller.Register(new RegisterUserDto
        {
            FullName = "Asha Karki",
            Email = "asha@example.com",
            Password = "Password1",
            Role = "JobSeeker"
        }, CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(result);
        userManager.Verify(manager => manager.CreateAsync(
            It.Is<User>(user => user.Email == "asha@example.com"), "Password1"), Times.Once);
        userManager.Verify(manager => manager.AddToRoleAsync(It.IsAny<User>(), "JobSeeker"), Times.Once);
    }

    private static UserController CreateController(
        UserManager<User> userManager,
        RoleManager<ApplicationRole>? roleManager = null,
        IUserService? userService = null,
        ICacheService? cache = null,
        IEmailService? email = null) =>
        new(
            userManager,
            roleManager ?? CreateRoleManager().Object,
            userService ?? new Mock<IUserService>().Object,
            cache ?? new Mock<ICacheService>().Object,
            email ?? new Mock<IEmailService>().Object,
            Options.Create(new JwtOptions
            {
                Key = "elevateworkforce-test-key-with-at-least-32-chars",
                Issuer = "ElevateWorkforce",
                Audience = "ElevateWorkforce.Api"
            }),
            NullLogger<UserController>.Instance);

    private static Mock<UserManager<User>> CreateUserManager() =>
        new(new Mock<IUserStore<User>>().Object, null!, null!, null!, null!, null!, null!, null!, null!);

    private static Mock<RoleManager<ApplicationRole>> CreateRoleManager() =>
        new(new Mock<IRoleStore<ApplicationRole>>().Object,
            Array.Empty<IRoleValidator<ApplicationRole>>(), null!, null!, null!);
}
