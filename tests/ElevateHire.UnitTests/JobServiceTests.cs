using ElevateHire.Application.Services;
using ElevateHire.Domain.Entities;
using ElevateHire.Domain.Enums;
using ElevateHire.Application.Interfaces;
using Moq;

namespace ElevateHire.UnitTests;

public class JobServiceTests
{
    private static List<Job> SampleJobs()
    {
        var company = new Company { Id = 1, Name = "Himalayan Tech" };

        return new List<Job>
        {
            new Job
            {
                Id = 1, Title = "Senior .NET Developer", Description = "Builds software", Status = JobStatus.Active,
                Deadline = null, Category = JobCategory.SoftwareDevelopment, Location = "Kathmandu",
                IsRemote = true, Company = company, MaxSalary = 150000, PublishedAt = DateTime.UtcNow,
                Skills = "C#, .NET",
                Applications = new List<JobApplication>()
            },
            new Job
            {
                Id = 2, Title = "Junior Designer", Description = "Designs UI", Status = JobStatus.Active,
                Deadline = DateTime.UtcNow.AddDays(5), Category = JobCategory.Design, Location = "Lalitpur",
                Company = company, MaxSalary = 40000, PublishedAt = DateTime.UtcNow.AddDays(-1),
                Skills = "Figma",
                Applications = new List<JobApplication>()
            },
            new Job
            {
                Id = 3, Title = "Expired Role", Description = "Sales work", Status = JobStatus.Active,
                Deadline = DateTime.UtcNow.AddDays(-1), Category = JobCategory.Sales, Location = "Kathmandu",
                Company = company, Skills = "Sales",
                Applications = new List<JobApplication>()
            },
            new Job
            {
                Id = 4, Title = "Pending Role", Description = "Marketing", Status = JobStatus.PendingVerification,
                Deadline = null, Category = JobCategory.Marketing, Location = "Pokhara",
                Company = company, Skills = "SEO",
                Applications = new List<JobApplication>()
            },
            new Job
            {
                Id = 5, Title = "Removed Role", Description = "Operations", Status = JobStatus.Removed, IsDeleted = true,
                Deadline = null, Category = JobCategory.Other, Location = "Kathmandu",
                Company = company, Skills = "Ops",
                Applications = new List<JobApplication>()
            }
        };
    }

    private static Mock<IJobRepository> RepositoryWith(List<Job> jobs)
    {
        var repo = new Mock<IJobRepository>();
        repo.Setup(r => r.Query()).Returns(jobs.AsTestAsync());
        return repo;
    }

    [Fact]
    public async Task SearchAsync_ReturnsOnlyActiveNonExpiredJobs()
    {
        var jobs = SampleJobs();
        var service = new JobService(RepositoryWith(jobs).Object);

        var result = await service.SearchAsync(null, null, null, null, null, null, null, null, null, "newest", 1, 10);

        Assert.Equal(2, result.Total);
        Assert.All(result.Items, j => Assert.True(j.IsAcceptingApplications));
    }

    [Fact]
    public async Task SearchAsync_FiltersByKeyword()
    {
        var jobs = SampleJobs();
        var service = new JobService(RepositoryWith(jobs).Object);

        var result = await service.SearchAsync("designer", null, null, null, null, null, null, null, null, "newest", 1, 10);

        Assert.Single(result.Items);
        Assert.Equal("Junior Designer", result.Items.First().Title);
    }

    [Fact]
    public async Task SearchAsync_FiltersByCategory()
    {
        var jobs = SampleJobs();
        var service = new JobService(RepositoryWith(jobs).Object);

        var result = await service.SearchAsync(null, null, "SoftwareDevelopment", null, null, null, null, null, null, "newest", 1, 10);

        Assert.Single(result.Items);
        Assert.Equal("Senior .NET Developer", result.Items.First().Title);
    }

    [Fact]
    public async Task SearchAsync_RespectsPagination()
    {
        var jobs = SampleJobs();
        var service = new JobService(RepositoryWith(jobs).Object);

        var page1 = await service.SearchAsync(null, null, null, null, null, null, null, null, null, "newest", 1, 1);
        var page2 = await service.SearchAsync(null, null, null, null, null, null, null, null, null, "newest", 2, 1);

        Assert.Single(page1.Items);
        Assert.Single(page2.Items);
        Assert.Equal(2, page1.TotalPages);
    }

    [Fact]
    public async Task ApproveAsync_SetsActiveAndPublishes()
    {
        var repo = new Mock<IJobRepository>();
        var job = new Job { Id = 9, Status = JobStatus.PendingVerification };
        repo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<bool>())).ReturnsAsync(job);
        var service = new JobService(repo.Object);

        await service.ApproveAsync(9);

        Assert.Equal(JobStatus.Active, job.Status);
        Assert.NotNull(job.PublishedAt);
        repo.Verify(r => r.UpdateAsync(job), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_PreservesCallerAssignedStatus()
    {
        var repo = new Mock<IJobRepository>();
        Job? captured = null;
        repo.Setup(r => r.AddAsync(It.IsAny<Job>()))
            .ReturnsAsync((Job j) => { captured = j; return j; });
        var service = new JobService(repo.Object);

        var job = new Job { Title = "Verified Post", Status = JobStatus.Active };
        await service.CreateAsync(job);

        Assert.Equal(JobStatus.Active, captured!.Status);
    }

    [Fact]
    public async Task RejectAsync_SetsRejected()
    {
        var repo = new Mock<IJobRepository>();
        var job = new Job { Id = 10, Status = JobStatus.PendingVerification };
        repo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<bool>())).ReturnsAsync(job);
        var service = new JobService(repo.Object);

        await service.RejectAsync(10, "Needs changes");

        Assert.Equal(JobStatus.Rejected, job.Status);
    }

    [Fact]
    public async Task SoftDeleteAsync_MarksJobDeleted()
    {
        var repo = new Mock<IJobRepository>();
        var job = new Job { Id = 11, Status = JobStatus.Active };
        repo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<bool>())).ReturnsAsync(job);
        var service = new JobService(repo.Object);

        var ok = await service.SoftDeleteAsync(11);

        Assert.True(ok);
        Assert.True(job.IsDeleted);
    }

    [Fact]
    public async Task CountByStatusAsync_ReturnsZeroForUnknownStatus()
    {
        var service = new JobService(RepositoryWith(SampleJobs()).Object);

        var count = await service.CountByStatusAsync("NotAStatus");

        Assert.Equal(0, count);
    }
}