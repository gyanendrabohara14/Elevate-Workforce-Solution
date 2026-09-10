using ElevateHire.Domain.Common;

namespace ElevateHire.Domain.Entities;

public class JobSeeker : BaseEntity
{
    public string? ProfessionalTitle { get; set; }
    public string? Location { get; set; }
    public string? Phone { get; set; }
    public string? About { get; set; }
    public string? Skills { get; set; }
    public string? Education { get; set; }
    public string? Experience { get; set; }
    public string? Certifications { get; set; }
    public string? Languages { get; set; }
    public string? ResumeFileName { get; set; }
    public string? ResumeContentType { get; set; }
    public long? ResumeSizeBytes { get; set; }
    public string? ResumeStoredName { get; set; }
    public DateTime? ResumeUploadedAt { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();
    public ICollection<SavedJob> SavedJobs { get; set; } = new List<SavedJob>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public int ProfileCompletionPercent()
    {
        int count = 0;
        int total = 9;
        if (!string.IsNullOrWhiteSpace(ProfessionalTitle)) count++;
        if (!string.IsNullOrWhiteSpace(Location)) count++;
        if (!string.IsNullOrWhiteSpace(Phone)) count++;
        if (!string.IsNullOrWhiteSpace(About)) count++;
        if (!string.IsNullOrWhiteSpace(Skills)) count++;
        if (!string.IsNullOrWhiteSpace(Education)) count++;
        if (!string.IsNullOrWhiteSpace(Experience)) count++;
        if (!string.IsNullOrWhiteSpace(Certifications)) count++;
        if (!string.IsNullOrWhiteSpace(ResumeStoredName)) count++;
        return (int)Math.Round((double)count / total * 100);
    }
}
