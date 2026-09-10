using ElevateHire.Domain.Entities;
using ElevateHire.Domain.Enums;

namespace ElevateHire.UnitTests;

public class DomainEntityTests
{
    [Fact]
    public void Job_IsExpired_True_WhenDeadlinePassed()
    {
        var job = new Job
        {
            Status = JobStatus.Active,
            Deadline = DateTime.UtcNow.AddDays(-1)
        };

        Assert.True(job.IsExpired);
        Assert.False(job.IsAcceptingApplications);
    }

    [Fact]
    public void Job_IsAcceptingApplications_RespectsStatusAndDeadline()
    {
        var active = new Job { Status = JobStatus.Active, Deadline = DateTime.UtcNow.AddDays(10) };
        var pending = new Job { Status = JobStatus.PendingVerification, Deadline = null };
        var closed = new Job { Status = JobStatus.Closed, Deadline = null };

        Assert.True(active.IsAcceptingApplications);
        Assert.False(pending.IsAcceptingApplications);
        Assert.False(closed.IsAcceptingApplications);
    }

    [Fact]
    public void Job_NoDeadline_IsNotExpired()
    {
        var job = new Job { Status = JobStatus.Active, Deadline = null };

        Assert.False(job.IsExpired);
        Assert.True(job.IsAcceptingApplications);
    }

    [Fact]
    public void JobSeeker_ProfileCompletionPercent_CountsFilledFields()
    {
        var empty = new JobSeeker();
        Assert.Equal(0, empty.ProfileCompletionPercent());

        var full = new JobSeeker
        {
            ProfessionalTitle = "Engineer",
            Location = "Kathmandu",
            Phone = "98xxxx",
            About = "About",
            Skills = "C#",
            Education = "BSc",
            Experience = "2y",
            Certifications = "AWS",
            ResumeStoredName = "resumes/a.pdf"
        };
        Assert.Equal(100, full.ProfileCompletionPercent());
    }

    [Fact]
    public void JobSeeker_ProfileCompletionPercent_Partial()
    {
        var partial = new JobSeeker { ProfessionalTitle = "Engineer", Skills = "C#", ResumeStoredName = "r.pdf" };

        Assert.Equal(33, partial.ProfileCompletionPercent());
    }

    [Fact]
    public void Job_StatusDisplay_ReflectsEnum()
    {
        Assert.Equal(nameof(JobStatus.PendingVerification), new Job { Status = JobStatus.PendingVerification }.StatusDisplay);
        Assert.Equal(nameof(JobStatus.Active), new Job { Status = JobStatus.Active }.StatusDisplay);
    }
}