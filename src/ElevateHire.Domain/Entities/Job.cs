using ElevateWorkforce.Domain.Common;
using ElevateWorkforce.Domain.Enums;

namespace ElevateWorkforce.Domain.Entities;

public class Job : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Responsibilities { get; set; }
    public string? Requirements { get; set; }
    public string? Qualifications { get; set; }
    public string? Skills { get; set; }
    public string? Benefits { get; set; }
    public string? Location { get; set; }
    public JobType JobType { get; set; } = JobType.FullTime;
    public JobCategory Category { get; set; } = JobCategory.Other;
    public ExperienceLevel ExperienceLevel { get; set; } = ExperienceLevel.Entry;
    public decimal? MinSalary { get; set; }
    public decimal? MaxSalary { get; set; }
    public bool IsRemote { get; set; }
    public bool IsHybrid { get; set; }
    public DateTime? Deadline { get; set; }
    public int? Vacancies { get; set; } = 1;
    public JobStatus Status { get; set; } = JobStatus.PendingVerification;
    public DateTime? PublishedAt { get; set; }
    public bool IsFeatured { get; set; }

    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();
    public ICollection<SavedJob> SavedJobs { get; set; } = new List<SavedJob>();

    public bool IsExpired => Deadline.HasValue && Deadline.Value < DateTime.UtcNow;
    public bool IsAcceptingApplications =>
        Status == JobStatus.Active && !IsExpired;

    public string StatusDisplay => Status.ToString();
}
