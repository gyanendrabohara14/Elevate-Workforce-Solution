using ElevateHire.Domain.Entities;

namespace ElevateHire.Web.ViewModels;

public class ErrorViewModel
{
    public int StatusCode { get; set; } = 500;
}

public class HomeViewModel
{
    public HomeStatsViewModel Stats { get; set; } = new();
    public IEnumerable<Job> FeaturedJobs { get; set; } = new List<Job>();
    public IEnumerable<string> Categories { get; set; } = new List<string>();
}

public class HomeStatsViewModel
{
    public int ActiveJobs { get; set; }
    public int Companies { get; set; }
    public int JobSeekers { get; set; }
    public int Applications { get; set; }
}

public class JobDetailsViewModel
{
    public Job Job { get; set; } = new();
    public bool IsSaved { get; set; }
    public bool HasApplied { get; set; }
}

public class SavedJobsViewModel
{
    public IEnumerable<Job> Items { get; set; } = new List<Job>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public int TotalPages { get; set; }
}

public class EmployerDashboardViewModel
{
    public Company Company { get; set; } = new();
    public int ActiveJobs { get; set; }
    public int PendingJobs { get; set; }
    public int TotalApplications { get; set; }
    public int Shortlisted { get; set; }
    public int Interviews { get; set; }
    public IEnumerable<JobApplication> RecentApplications { get; set; } = new List<JobApplication>();
    public IEnumerable<Job> Jobs { get; set; } = new List<Job>();
}

public class JobListViewModel
{
    public Company Company { get; set; } = new();
    public IEnumerable<Job> Jobs { get; set; } = new List<Job>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public int TotalPages { get; set; }
}

public class EmployerApplicantsViewModel
{
    public int FilterJobId { get; set; }
    public string? Status { get; set; }
    public IEnumerable<Job> JobOptions { get; set; } = new List<Job>();
    public IEnumerable<JobApplication> Applications { get; set; } = new List<JobApplication>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public int TotalPages { get; set; }
}

public class JobSeekerDashboardViewModel
{
    public JobSeeker Profile { get; set; } = new();
    public int TotalApplications { get; set; }
    public int UnderReview { get; set; }
    public int Shortlisted { get; set; }
    public int Interviews { get; set; }
    public int SavedJobs { get; set; }
    public int ProfileCompletion { get; set; }
    public IEnumerable<JobApplication> RecentApplications { get; set; } = new List<JobApplication>();
    public IEnumerable<Job> RecommendedJobs { get; set; } = new List<Job>();
    public IEnumerable<SavedJob> SavedJobItems { get; set; } = new List<SavedJob>();
    public IEnumerable<Notification> Notifications { get; set; } = new List<Notification>();
}

public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int JobSeekers { get; set; }
    public int Employers { get; set; }
    public int Companies { get; set; }
    public int PendingCompanies { get; set; }
    public int TotalJobs { get; set; }
    public int PendingJobs { get; set; }
    public int ActiveJobs { get; set; }
    public int Applications { get; set; }
    public IEnumerable<Job> PendingJobsList { get; set; } = new List<Job>();
    public IEnumerable<Company> RecentCompanies { get; set; } = new List<Company>();
    public IEnumerable<User> RecentUsers { get; set; } = new List<User>();
}

public class AdminJobsViewModel
{
    public string? Keyword { get; set; }
    public string? Status { get; set; }
    public IEnumerable<Job> Jobs { get; set; } = new List<Job>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public int TotalPages { get; set; }
}