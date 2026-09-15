using ElevateWorkforce.Domain.Common;

namespace ElevateWorkforce.Domain.Entities;

public class Notification : BaseEntity
{
    public int JobSeekerId { get; set; }
    public JobSeeker JobSeeker { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Link { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}
