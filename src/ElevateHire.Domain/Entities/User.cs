using ElevateWorkforce.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace ElevateWorkforce.Domain.Entities;

public class User : IdentityUser<int>
{
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public JobSeeker? JobSeeker { get; set; }
    public Employer? Employer { get; set; }
}
