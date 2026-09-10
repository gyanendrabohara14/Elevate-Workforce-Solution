using System.ComponentModel.DataAnnotations;
using ElevateHire.Domain.Enums;

namespace ElevateHire.Web.ViewModels;

public class JobFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Job title is required.")]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Category")]
    public JobCategory Category { get; set; } = JobCategory.SoftwareDevelopment;

    [Required(ErrorMessage = "Job type is required.")]
    [Display(Name = "Employment type")]
    public JobType JobType { get; set; } = JobType.FullTime;

    [Display(Name = "Experience level")]
    public ExperienceLevel ExperienceLevel { get; set; } = ExperienceLevel.Entry;

    [Display(Name = "Remote")]
    public bool IsRemote { get; set; }

    [Display(Name = "Hybrid")]
    public bool IsHybrid { get; set; }

    [Display(Name = "Location")]
    public string? Location { get; set; }

    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Responsibilities")]
    public string? Responsibilities { get; set; }

    [Display(Name = "Requirements")]
    public string? Requirements { get; set; }

    [Display(Name = "Qualifications")]
    public string? Qualifications { get; set; }

    [Display(Name = "Skills (comma separated)")]
    public string? Skills { get; set; }

    [Display(Name = "Benefits")]
    public string? Benefits { get; set; }

    [Range(0, 10000000, ErrorMessage = "Enter a valid minimum salary.")]
    [Display(Name = "Minimum monthly salary (NPR)")]
    public decimal? MinSalary { get; set; }

    [Range(0, 10000000, ErrorMessage = "Enter a valid maximum salary.")]
    [Display(Name = "Maximum monthly salary (NPR)")]
    public decimal? MaxSalary { get; set; }

    [Display(Name = "Number of vacancies")]
    [Range(1, 500, ErrorMessage = "Vacancies must be between 1 and 500.")]
    public int? Vacancies { get; set; } = 1;

    [DataType(DataType.Date)]
    [Display(Name = "Application deadline")]
    public DateTime? Deadline { get; set; }
}

public class JobSearchViewModel
{
    public string? Keyword { get; set; }
    public string? Location { get; set; }
    public string? Category { get; set; }
    public string? JobType { get; set; }
    public string? Experience { get; set; }
    public decimal? MinSalary { get; set; }
    public decimal? MaxSalary { get; set; }
    public string? Posted { get; set; }
    public bool? Remote { get; set; }
    public string? Sort { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 9;
    public int Total { get; set; }
    public int TotalPages { get; set; }
    public IEnumerable<ElevateHire.Domain.Entities.Job> Jobs { get; set; } = new List<ElevateHire.Domain.Entities.Job>();
    public IEnumerable<string> Categories { get; set; } = Enum.GetNames<JobCategory>();
    public IEnumerable<string> JobTypes { get; set; } = Enum.GetNames<JobType>();
    public IEnumerable<string> ExperienceLevels { get; set; } = Enum.GetNames<ExperienceLevel>();
    public IDictionary<int, bool> SavedIds { get; set; } = new Dictionary<int, bool>();
}

public class ApplyViewModel
{
    public int JobId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;

    public bool HasResume { get; set; }

    [Display(Name = "Use my uploaded resume")]
    public bool UseUploadedResume { get; set; } = true;

    public IFormFile? Resume { get; set; }

    [Display(Name = "Cover letter")]
    [MaxLength(3000, ErrorMessage = "Cover letter must be under 3000 characters.")]
    public string? CoverLetter { get; set; }
}

public class JobSeekerProfileViewModel
{
    [Required(ErrorMessage = "Full name is required.")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Professional title")]
    public string? ProfessionalTitle { get; set; }

    [Display(Name = "Location")]
    public string? Location { get; set; }

    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    [Display(Name = "About")]
    public string? About { get; set; }

    [Display(Name = "Skills (comma separated)")]
    public string? Skills { get; set; }

    [Display(Name = "Education")]
    public string? Education { get; set; }

    [Display(Name = "Experience")]
    public string? Experience { get; set; }

    [Display(Name = "Certifications")]
    public string? Certifications { get; set; }

    [Display(Name = "Languages")]
    public string? Languages { get; set; }

    public IFormFile? Resume { get; set; }
}

public class CompanyFormViewModel
{
    [Required(ErrorMessage = "Company name is required.")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Industry")]
    public string? Industry { get; set; }

    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Website")]
    [Url]
    public string? Website { get; set; }

    [Display(Name = "Head office location")]
    public string? Location { get; set; }

    [Display(Name = "Contact email")]
    [EmailAddress]
    public string? ContactEmail { get; set; }

    [Display(Name = "Contact phone")]
    public string? ContactPhone { get; set; }

    public IFormFile? Logo { get; set; }
}

public class CompanyListViewModel
{
    public string? Keyword { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int Total { get; set; }
    public int TotalPages { get; set; }
    public IEnumerable<ElevateHire.Domain.Entities.Company> Companies { get; set; } = new List<ElevateHire.Domain.Entities.Company>();
}

public class UserListViewModel
{
    public string? Keyword { get; set; }
    public string? Role { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int Total { get; set; }
    public int TotalPages { get; set; }
    public IEnumerable<ElevateHire.Domain.Entities.User> Users { get; set; } = new List<ElevateHire.Domain.Entities.User>();
}

public class ApplicationsDashboardListViewModel
{
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int Total { get; set; }
    public int TotalPages { get; set; }
    public IEnumerable<ElevateHire.Domain.Entities.JobApplication> Applications { get; set; } = new List<ElevateHire.Domain.Entities.JobApplication>();
}