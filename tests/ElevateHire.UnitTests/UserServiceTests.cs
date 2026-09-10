using ElevateHire.Application.Services;
using ElevateHire.Domain.Entities;
using ElevateHire.Application.Interfaces;
using Moq;

namespace ElevateHire.UnitTests;

public class UserServiceTests
{
    [Fact]
    public async Task EnsureJobSeekerAsync_CreatesProfileWhenMissing()
    {
        var userRepo = new Mock<IUserRepository>();
        var employerRepo = new Mock<IEmployerRepository>();
        var companyRepo = new Mock<ICompanyRepository>();
        userRepo.Setup(r => r.GetByUserIdAsync(It.IsAny<int>())).ReturnsAsync((JobSeeker?)null);
        userRepo.Setup(r => r.AddAsync(It.IsAny<JobSeeker>()))
            .ReturnsAsync((JobSeeker js) => { js.Id = 99; return js; });

        var service = new UserService(userRepo.Object, employerRepo.Object, companyRepo.Object);
        var user = new User { Id = 7 };

        var seeker = await service.EnsureJobSeekerAsync(user);

        Assert.Equal(7, seeker.UserId);
        userRepo.Verify(r => r.AddAsync(It.Is<JobSeeker>(js => js.UserId == 7)), Times.Once);
    }

    [Fact]
    public async Task EnsureJobSeekerAsync_ReturnsExistingProfileWhenPresent()
    {
        var userRepo = new Mock<IUserRepository>();
        var employerRepo = new Mock<IEmployerRepository>();
        var companyRepo = new Mock<ICompanyRepository>();
        var existing = new JobSeeker { Id = 5, UserId = 7 };
        userRepo.Setup(r => r.GetByUserIdAsync(7)).ReturnsAsync(existing);

        var service = new UserService(userRepo.Object, employerRepo.Object, companyRepo.Object);

        var seeker = await service.EnsureJobSeekerAsync(new User { Id = 7 });

        Assert.Same(existing, seeker);
        userRepo.Verify(r => r.AddAsync(It.IsAny<JobSeeker>()), Times.Never);
    }
}