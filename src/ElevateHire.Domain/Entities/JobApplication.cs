using ElevateWorkforce.Domain.Common;
using ElevateWorkforce.Domain.Enums;

namespace ElevateWorkforce.Domain.Entities;

public class JobApplication : BaseEntity
{
    public string? CoverLetter { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Applied;
    public DateTime? ReviewedAt { get; set; }
    public DateTime? ShortlistedAt { get; set; }
    public DateTime? InterviewAt { get; set; }

    public int JobId { get; set; }
    public Job Job { get; set; } = null!;

    public int JobSeekerId { get; set; }
    public JobSeeker JobSeeker { get; set; } = null!;

    public ICollection<JobApplicationTimelineEntry> Timeline { get; set; } = new List<JobApplicationTimelineEntry>();
}