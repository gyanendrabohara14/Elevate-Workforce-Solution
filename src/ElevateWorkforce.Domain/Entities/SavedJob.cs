using ElevateWorkforce.Domain.Common;

namespace ElevateWorkforce.Domain.Entities;

public class SavedJob : BaseEntity
{
    public int JobSeekerId { get; set; }
    public JobSeeker JobSeeker { get; set; } = null!;
    public int JobId { get; set; }
    public Job Job { get; set; } = null!;
}
