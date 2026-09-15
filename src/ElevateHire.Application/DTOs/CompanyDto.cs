using ElevateWorkforce.Domain.Enums;

namespace ElevateWorkforce.Application.DTOs;

public class CompanyDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Industry { get; set; }
    public string? Description { get; set; }
    public string? Website { get; set; }
    public string? Location { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public CompanyStatus Status { get; set; }
    public int JobCount { get; set; }
    public string? EmployerName { get; set; }
}
