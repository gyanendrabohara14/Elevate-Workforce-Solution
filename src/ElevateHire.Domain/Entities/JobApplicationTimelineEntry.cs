using ElevateWorkforce.Domain.Common;
using ElevateWorkforce.Domain.Enums;

namespace ElevateWorkforce.Domain.Entities;

public class JobApplicationTimelineEntry : BaseEntity
{
    public int ApplicationId { get; set; }
    public JobApplication Application { get; set; } = null!;
    public ApplicationStatus Status { get; set; }
    public string? Note { get; set; }
    public string ChangedByName { get; set; } = string.Empty;
}