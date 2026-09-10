using ElevateHire.Domain.Common;

namespace ElevateHire.Domain.Entities;

public class Employer : BaseEntity
{
    public string? Position { get; set; }
    public string? Phone { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
}
