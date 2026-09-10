using ElevateHire.Application.Interfaces;
using ElevateHire.Application.Services;
using ElevateHire.Domain.Entities;
using ElevateHire.Domain.Enums;
using Moq;

namespace ElevateHire.UnitTests;

public class ApplicationServiceTests
{
    private static (ApplicationService Service, Mock<IApplicationRepository> Repository,
        Mock<IJobSeekerRepository> Seekers, Mock<INotificationRepository> Notifications) CreateService()
    {
        var repo = new Mock<IApplicationRepository>();
        var seekers = new Mock<IJobSeekerRepository>();
        var notifications = new Mock<INotificationRepository>();
        var service = new ApplicationService(repo.Object, seekers.Object, notifications.Object);
        return (service, repo, seekers, notifications);
    }

    [Fact]
    public async Task UpdateStatusAsync_ChangesStatus_AddsTimelineAndNotifies()
    {
        var (service, repo, seekers, _) = CreateService();
        var application = new JobApplication
        {
            Id = 1,
            JobSeekerId = 7,
            Status = ApplicationStatus.Applied,
            Timeline = new List<JobApplicationTimelineEntry>(),
            Job = new Job { Title = "Engineer" }
        };
        repo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<bool>())).ReturnsAsync(application);

        await service.UpdateStatusAsync(1, ApplicationStatus.Shortlisted, "Great fit", "HR Team");

        Assert.Equal(ApplicationStatus.Shortlisted, application.Status);
        Assert.NotNull(application.ShortlistedAt);
        Assert.Single(application.Timeline);
        Assert.Equal("Great fit", application.Timeline.First().Note);
        Assert.Equal("HR Team", application.Timeline.First().ChangedByName);
        seekers.Verify(s => s.AddNotificationAsync(It.Is<Notification>(n => n.Title == "You have been shortlisted")), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_Interview_SetsInterviewAt()
    {
        var (service, repo, _, _) = CreateService();
        var application = new JobApplication
        {
            Id = 2,
            JobSeekerId = 7,
            Status = ApplicationStatus.Shortlisted,
            Timeline = new List<JobApplicationTimelineEntry>(),
            Job = new Job { Title = "Engineer" }
        };
        repo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<bool>())).ReturnsAsync(application);

        await service.UpdateStatusAsync(2, ApplicationStatus.Interview, null, "HR");

        Assert.Equal(ApplicationStatus.Interview, application.Status);
        Assert.NotNull(application.InterviewAt);
    }

    [Fact]
    public async Task WithdrawAsync_ReturnsTrue_ForActiveApplication()
    {
        var (service, repo, _, _) = CreateService();
        var application = new JobApplication
        {
            Id = 3,
            Status = ApplicationStatus.Applied,
            Timeline = new List<JobApplicationTimelineEntry>()
        };
        repo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<bool>())).ReturnsAsync(application);

        var ok = await service.WithdrawAsync(3);

        Assert.True(ok);
        Assert.Equal(ApplicationStatus.Withdrawn, application.Status);
        repo.Verify(r => r.UpdateAsync(application), Times.Once);
    }

    [Fact]
    public async Task WithdrawAsync_ReturnsFalse_WhenAlreadySelected()
    {
        var (service, repo, _, _) = CreateService();
        var application = new JobApplication
        {
            Id = 4,
            Status = ApplicationStatus.Selected,
            Timeline = new List<JobApplicationTimelineEntry>()
        };
        repo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<bool>())).ReturnsAsync(application);

        var ok = await service.WithdrawAsync(4);

        Assert.False(ok);
        Assert.Equal(ApplicationStatus.Selected, application.Status);
    }

    [Fact]
    public async Task HasAppliedAsync_ReturnsTrue_WhenApplicationExists()
    {
        var (service, repo, _, _) = CreateService();
        var applications = new List<JobApplication>
        {
            new() { Id = 1, JobSeekerId = 7, JobId = 9, Job = new Job(), JobSeeker = new JobSeeker(), Timeline = new List<JobApplicationTimelineEntry>() }
        };
        repo.Setup(r => r.Query()).Returns(applications.AsTestAsync());

        var applied = await service.HasAppliedAsync(7, 9);
        var notApplied = await service.HasAppliedAsync(7, 999);

        Assert.True(applied);
        Assert.False(notApplied);
    }

    [Fact]
    public async Task GetForJobSeekerAsync_ReturnsOnlyThatSeekersApplications()
    {
        var (service, repo, _, _) = CreateService();
        var applications = new List<JobApplication>
        {
            new() { Id = 1, JobSeekerId = 7, Job = new Job { Title = "A" }, Timeline = new List<JobApplicationTimelineEntry>() },
            new() { Id = 2, JobSeekerId = 7, Job = new Job { Title = "B" }, Timeline = new List<JobApplicationTimelineEntry>() },
            new() { Id = 3, JobSeekerId = 8, Job = new Job { Title = "C" }, Timeline = new List<JobApplicationTimelineEntry>() }
        };
        repo.Setup(r => r.Query()).Returns(applications.AsTestAsync());

        var result = await service.GetForJobSeekerAsync(7, 1, 10);

        Assert.Equal(2, result.Total);
        Assert.All(result.Items, a => Assert.Equal(7, a.JobSeekerId));
    }

    [Fact]
    public async Task CreateAsync_SetsAppliedStatus_AndTimelineEntry()
    {
        var (service, repo, seekers, _) = CreateService();
        var created = new JobApplication
        {
            Id = 5,
            JobSeekerId = 7,
            JobId = 9,
            Status = ApplicationStatus.Applied,
            Timeline = new List<JobApplicationTimelineEntry>(),
            Job = new Job { Title = "Designer" },
            JobSeeker = new JobSeeker { User = new User { FullName = "Anita" } }
        };
        repo.Setup(r => r.AddAsync(It.IsAny<JobApplication>())).ReturnsAsync(created);

        var result = await service.CreateAsync(new JobApplication
        {
            JobId = 9,
            JobSeekerId = 7,
            Job = created.Job,
            JobSeeker = created.JobSeeker
        });

        Assert.Equal(ApplicationStatus.Applied, result.Status);
        Assert.Single(result.Timeline);
        Assert.Equal("Application submitted.", result.Timeline.First().Note);
        seekers.Verify(s => s.AddNotificationAsync(It.Is<Notification>(n => n.JobSeekerId == 7)), Times.Once);
    }
}