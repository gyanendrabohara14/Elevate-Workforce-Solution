using ElevateWorkforce.Domain.Enums;

namespace ElevateWorkforce.Application.DTOs;

public class JobDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Responsibilities { get; set; }
    public string? Requirements { get; set; }
    public string? Qualifications { get; set; }
    public string? Skills { get; set; }
    public string? Benefits { get; set; }
    public string? Location { get; set; }
    public JobType JobType { get; set; }
    public JobCategory Category { get; set; }
    public ExperienceLevel ExperienceLevel { get; set; }
    public decimal? MinSalary { get; set; }
    public decimal? MaxSalary { get; set; }
    public bool IsRemote { get; set; }
    public bool IsHybrid { get; set; }
    public DateTime? Deadline { get; set; }
    public int? Vacancies { get; set; }
    public JobStatus Status { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? CompanyLogo { get; set; }
    public DateTime PostedAt { get; set; }
    public int ApplicationsCount { get; set; }
}
