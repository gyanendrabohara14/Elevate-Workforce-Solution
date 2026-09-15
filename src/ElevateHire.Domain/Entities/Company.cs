using ElevateWorkforce.Domain.Common;
using ElevateWorkforce.Domain.Enums;

namespace ElevateWorkforce.Domain.Entities;

public class Company : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Industry { get; set; }
    public string? Description { get; set; }
    public string? Website { get; set; }
    public string? Location { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? LogoFileName { get; set; }
    public string? LogoContentType { get; set; }
    public string? LogoStoredName { get; set; }
    public CompanyStatus Status { get; set; } = CompanyStatus.PendingVerification;

    public ICollection<Employer> Employers { get; set; } = new List<Employer>();
    public ICollection<Job> Jobs { get; set; } = new List<Job>();

    public string StatusDisplay => Status.ToString();
}
