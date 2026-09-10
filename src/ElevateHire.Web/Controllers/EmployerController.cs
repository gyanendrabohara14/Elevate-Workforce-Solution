using ElevateHire.Application.AI;
using ElevateHire.Application.Interfaces;
using ElevateHire.Domain.Entities;
using ElevateHire.Domain.Enums;
using ElevateHire.Infrastructure.Storage;
using ElevateHire.Web.Services;
using ElevateHire.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElevateHire.Web.Controllers;

[Authorize(Roles = "Employer,Admin")]
public class EmployerController : Controller
{
    private static readonly string[] LogoExtensions = { ".png", ".jpg", ".jpeg", ".svg", ".webp" };
    private const long MaxLogoBytes = 2 * 1024 * 1024;

    private readonly UserManager<User> _userManager;
    private readonly IUserService _userService;
    private readonly ICompanyService _companyService;
    private readonly IJobService _jobService;
    private readonly IApplicationService _applicationService;
    private readonly IAIService _aiService;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<EmployerController> _logger;

    public EmployerController(
        UserManager<User> userManager,
        IUserService userService,
        ICompanyService companyService,
        IJobService jobService,
        IApplicationService applicationService,
        IAIService aiService,
        IFileStorageService fileStorage,
        ILogger<EmployerController> logger)
    {
        _userManager = userManager;
        _userService = userService;
        _companyService = companyService;
        _jobService = jobService;
        _applicationService = applicationService;
        _aiService = aiService;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task<IActionResult> Dashboard()
    {
        var employer = await CurrentEmployerAsync();
        if (employer is null)
            return RedirectToAction(nameof(Company), new { firstTime = "true" });

        var company = await _companyService.GetByIdAsync(employer.CompanyId);
        if (company is null)
            return RedirectToAction(nameof(Company));

        var jobsResult = await _jobService.GetByCompanyAsync(company.Id, 1, 50);
        var companyJobs = jobsResult.Items.ToList();
        var activeJobs = companyJobs.Count(j => j.Status == JobStatus.Active && !j.IsExpired);
        var pendingJobs = companyJobs.Count(j => j.Status == JobStatus.PendingVerification);
        var totalApplications = await _applicationService.CountForCompanyAsync(company.Id);
        var shortlisted = await _applicationService.CountForCompanyByStatusAsync(company.Id, ApplicationStatus.Shortlisted);
        var interviews = await _applicationService.CountForCompanyByStatusAsync(company.Id, ApplicationStatus.Interview);

        var recentApplications = (await _applicationService.GetRecentForCompanyAsync(company.Id, 8)).ToList();

        return View(new EmployerDashboardViewModel
        {
            Company = company,
            ActiveJobs = activeJobs,
            PendingJobs = pendingJobs,
            TotalApplications = totalApplications,
            Shortlisted = shortlisted,
            Interviews = interviews,
            RecentApplications = recentApplications,
            Jobs = companyJobs.OrderByDescending(j => j.CreatedAt).Take(6)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Company(string? firstTime)
    {
        var employer = await CurrentEmployerAsync();
        if (employer is null)
            return View("CreateCompany");

        var company = await _companyService.GetByIdAsync(employer.CompanyId);
        if (company is null)
            return View("CreateCompany");

        ViewBag.FirstTime = firstTime == "true";
        ViewBag.CompanyStatus = company.Status;
        return View(new CompanyFormViewModel
        {
            Name = company.Name,
            Industry = company.Industry,
            Description = company.Description,
            Website = company.Website,
            Location = company.Location,
            ContactEmail = company.ContactEmail,
            ContactPhone = company.ContactPhone
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Company(CompanyFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var employer = await EnsureEmployerAsync();

        var company = employer.CompanyId == 0
            ? new Company { Status = CompanyStatus.PendingVerification }
            : await _companyService.GetByIdAsync(employer.CompanyId);

        if (company is null)
            company = new Company { Status = CompanyStatus.PendingVerification };

        company.Name = model.Name.Trim();
        company.Industry = model.Industry?.Trim();
        company.Description = model.Description?.Trim();
        company.Website = model.Website?.Trim();
        company.Location = model.Location?.Trim();
        company.ContactEmail = model.ContactEmail?.Trim();
        company.ContactPhone = model.ContactPhone?.Trim();

        if (employer.CompanyId == 0 || company.Id == 0)
        {
            var created = await _companyService.CreateAsync(company);
            employer.CompanyId = created.Id;
            employer.Position = "HR";
            await _userService.UpdateEmployerAsync(employer);
        }
        else
        {
            await _companyService.UpdateAsync(company);
        }

        if (model.Logo is not null && IsValidLogo(model.Logo))
        {
            await using var stream = model.Logo.OpenReadStream();
            var (storedName, _) = await _fileStorage.SaveAsync(stream, model.Logo.FileName, "company-logos", LogoExtensions, MaxLogoBytes);
            await _companyService.SetLogoAsync(company, storedName, model.Logo.FileName, model.Logo.ContentType ?? "image/png", model.Logo.Length);
        }

        TempData["success"] = "Company profile saved. It is now awaiting verification by an administrator.";
        return RedirectToAction(nameof(Dashboard));
    }

    [HttpGet]
    public async Task<IActionResult> Jobs(int page = 1)
    {
        var employer = await CurrentEmployerAsync();
        if (employer is null || employer.CompanyId == 0)
            return RedirectToAction(nameof(Company));

        var company = await _companyService.GetByIdAsync(employer.CompanyId);
        if (company is null)
            return RedirectToAction(nameof(Company));

        var result = await _jobService.GetByCompanyAsync(company.Id, page, 10);
        return View(new JobListViewModel
        {
            Company = company,
            Jobs = result.Items,
            Page = page,
            PageSize = 10,
            Total = result.Total,
            TotalPages = result.TotalPages
        });
    }

    [HttpGet]
    public async Task<IActionResult> CreateJob()
    {
        var employer = await CurrentEmployerAsync();
        if (employer is null || employer.CompanyId == 0)
            return RedirectToAction(nameof(Company));

        var company = await _companyService.GetByIdAsync(employer.CompanyId);
        if (company is null)
            return RedirectToAction(nameof(Company));

        ViewData["CompanyStatus"] = company.Status;
        return View(new JobFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateJob(JobFormViewModel model)
    {
        var employer = await CurrentEmployerAsync();
        if (employer is null || employer.CompanyId == 0)
            return RedirectToAction(nameof(Company));

        if (!ModelState.IsValid)
            return View(model);

        var company = await _companyService.GetByIdAsync(employer.CompanyId);

        var job = new Job
        {
            Title = model.Title.Trim(),
            Category = model.Category,
            JobType = model.JobType,
            ExperienceLevel = model.ExperienceLevel,
            Location = model.Location?.Trim(),
            IsRemote = model.IsRemote,
            IsHybrid = model.IsHybrid,
            Description = model.Description?.Trim(),
            Responsibilities = model.Responsibilities?.Trim(),
            Requirements = model.Requirements?.Trim(),
            Qualifications = model.Qualifications?.Trim(),
            Skills = model.Skills?.Trim(),
            Benefits = model.Benefits?.Trim(),
            MinSalary = model.MinSalary,
            MaxSalary = model.MaxSalary,
            Vacancies = model.Vacancies ?? 1,
            Deadline = model.Deadline.HasValue ? DateTime.SpecifyKind(model.Deadline.Value, DateTimeKind.Utc) : null,
            CompanyId = employer.CompanyId,
            Status = company?.Status == CompanyStatus.Active ? JobStatus.Active : JobStatus.PendingVerification,
            PublishedAt = company?.Status == CompanyStatus.Active ? DateTime.UtcNow : null
        };

        if (job.Deadline.HasValue && job.Deadline.Value < DateTime.UtcNow)
        {
            job.Deadline = null;
        }

        await _jobService.CreateAsync(job);

        TempData["success"] = job.Status == JobStatus.Active
            ? "Your job is now live and visible to everyone."
            : "Your job was submitted. It will go live once your company profile is verified.";
        return RedirectToAction(nameof(Jobs));
    }

    [HttpGet]
    public async Task<IActionResult> EditJob(int id)
    {
        var employer = await CurrentEmployerAsync();
        if (employer is null || employer.CompanyId == 0)
            return RedirectToAction(nameof(Company));

        if (!await _jobService.IsOwnedByCompanyAsync(id, employer.CompanyId))
            return Forbid();

        var job = await _jobService.GetByIdAsync(id);
        if (job is null)
            return NotFound();

        return View(new JobFormViewModel
        {
            Id = job.Id,
            Title = job.Title,
            Category = job.Category,
            JobType = job.JobType,
            ExperienceLevel = job.ExperienceLevel,
            Location = job.Location,
            IsRemote = job.IsRemote,
            IsHybrid = job.IsHybrid,
            Description = job.Description,
            Responsibilities = job.Responsibilities,
            Requirements = job.Requirements,
            Qualifications = job.Qualifications,
            Skills = job.Skills,
            Benefits = job.Benefits,
            MinSalary = job.MinSalary,
            MaxSalary = job.MaxSalary,
            Vacancies = job.Vacancies,
            Deadline = job.Deadline
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditJob(JobFormViewModel model)
    {
        var employer = await CurrentEmployerAsync();
        if (employer is null || employer.CompanyId == 0)
            return RedirectToAction(nameof(Company));

        if (!await _jobService.IsOwnedByCompanyAsync(model.Id, employer.CompanyId))
            return Forbid();

        var job = await _jobService.GetByIdAsync(model.Id);
        if (job is null)
            return NotFound();

        job.Title = model.Title.Trim();
        job.Category = model.Category;
        job.JobType = model.JobType;
        job.ExperienceLevel = model.ExperienceLevel;
        job.Location = model.Location?.Trim();
        job.IsRemote = model.IsRemote;
        job.IsHybrid = model.IsHybrid;
        job.Description = model.Description?.Trim();
        job.Responsibilities = model.Responsibilities?.Trim();
        job.Requirements = model.Requirements?.Trim();
        job.Qualifications = model.Qualifications?.Trim();
        job.Skills = model.Skills?.Trim();
        job.Benefits = model.Benefits?.Trim();
        job.MinSalary = model.MinSalary;
        job.MaxSalary = model.MaxSalary;
        job.Vacancies = model.Vacancies ?? 1;
        job.Deadline = model.Deadline.HasValue ? DateTime.SpecifyKind(model.Deadline.Value, DateTimeKind.Utc) : null;

        if (job.Status == JobStatus.Rejected)
            job.Status = JobStatus.PendingVerification;

        await _jobService.UpdateAsync(job);

        TempData["success"] = "Job updated.";
        return RedirectToAction(nameof(Jobs));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteJob(int id)
    {
        var employer = await CurrentEmployerAsync();
        if (employer is null || employer.CompanyId == 0)
            return RedirectToAction(nameof(Company));

        if (!await _jobService.IsOwnedByCompanyAsync(id, employer.CompanyId))
            return Forbid();

        await _jobService.SoftDeleteAsync(id);
        TempData["success"] = "Job removed.";
        return RedirectToAction(nameof(Jobs));
    }

    [HttpGet]
    public async Task<IActionResult> Applicants(int jobId, string? status, int page = 1)
    {
        var employer = await CurrentEmployerAsync();
        if (employer is null || employer.CompanyId == 0)
            return RedirectToAction(nameof(Company));

        if (jobId > 0 && !await _jobService.IsOwnedByCompanyAsync(jobId, employer.CompanyId))
            return Forbid();

        var result = await _applicationService.GetForCompanyAsync(employer.CompanyId, page, 10, status);

        var view = new EmployerApplicantsViewModel
        {
            FilterJobId = jobId,
            Status = status,
            Page = page,
            PageSize = 10,
            Total = result.Total,
            TotalPages = result.TotalPages,
            Applications = result.Items
        };

        var company = await _companyService.GetByIdAsync(employer.CompanyId);
        view.JobOptions = company is not null
            ? (await _jobService.GetByCompanyAsync(company.Id, 1, 100)).Items
            : Enumerable.Empty<Job>();

        return View(view);
    }

    [HttpGet]
    public async Task<IActionResult> Applicant(int id)
    {
        var employer = await CurrentEmployerAsync();
        if (employer is null || employer.CompanyId == 0)
            return RedirectToAction(nameof(Company));

        var application = await _applicationService.GetByIdAsync(id);
        if (application is null || application.Job?.CompanyId != employer.CompanyId)
            return NotFound();

        return View(application);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateApplicationStatus(int id, ApplicationStatus status, string? note)
    {
        var employer = await CurrentEmployerAsync();
        if (employer is null || employer.CompanyId == 0)
            return RedirectToAction(nameof(Company));

        var application = await _applicationService.GetByIdAsync(id);
        if (application is null || application.Job?.CompanyId != employer.CompanyId)
            return NotFound();

        var changedByName = $"{employer.User?.FullName} ({employer.User?.Email})";
        await _applicationService.UpdateStatusAsync(id, status, note, changedByName);

        TempData["success"] = $"Application moved to {status}.";
        return RedirectToAction(nameof(Applicant), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Insights(int id)
    {
        var employer = await CurrentEmployerAsync();
        if (employer is null || employer.CompanyId == 0)
            return RedirectToAction(nameof(Company));

        var application = await _applicationService.GetByIdAsync(id);
        if (application is null || application.Job?.CompanyId != employer.CompanyId)
            return NotFound();

        var candidate = application.JobSeeker;
        var job = application.Job;

        var profile = string.Join(" | ",
            candidate?.ProfessionalTitle, candidate?.About, candidate?.Skills,
            candidate?.Education, candidate?.Experience, candidate?.Certifications);

        var requirements = job?.Requirements;
        var description = job?.Description + Environment.NewLine + job?.Responsibilities;

        ResumeMatchingResponse insights;
        if (_aiService.IsEnabled)
        {
            insights = await _aiService.GetCandidateInsightsAsync(new ResumeMatchingRequest
            {
                CandidateProfile = profile ?? "Candidate profile incomplete.",
                JobRequirements = requirements ?? "No explicit requirements listed.",
                JobDescription = description ?? "No job description available."
            });
        }
        else
        {
            insights = new ResumeMatchingResponse { Success = false, Error = "AI assistance is temporarily unavailable. You can continue manually." };
        }

        ViewBag.Application = application;
        return View(insights);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadResume(int id)
    {
        var employer = await CurrentEmployerAsync();
        if (employer is null || employer.CompanyId == 0)
            return RedirectToAction(nameof(Company));

        var application = await _applicationService.GetByIdAsync(id);
        if (application is null || application.Job?.CompanyId != employer.CompanyId)
            return NotFound();

        return await ResumeAsync(application);
    }

    private async Task<IActionResult> ResumeAsync(JobApplication application)
    {
        if (application.JobSeeker is null)
            application.JobSeeker = (await _userService.GetJobSeekerByIdAsync(application.JobSeekerId))!;

        if (application.JobSeeker is null || string.IsNullOrEmpty(application.JobSeeker.ResumeStoredName))
            return NotFound();

        var path = _fileStorage.ResolvePath(application.JobSeeker.ResumeStoredName, "resumes");
        if (!System.IO.File.Exists(path))
            return NotFound();

        return File(
            await System.IO.File.ReadAllBytesAsync(path),
            application.JobSeeker.ResumeContentType ?? "application/octet-stream",
            application.JobSeeker.ResumeFileName ?? "resume.pdf");
    }

    private async Task<Employer?> CurrentEmployerAsync()
    {
        var idValue = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(idValue, out var userId))
            return null;
        return await _userService.GetEmployerByUserIdAsync(userId);
    }

    private async Task<Employer> EnsureEmployerAsync()
    {
        var existing = await CurrentEmployerAsync();
        if (existing is not null) return existing;

        var idValue = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var user = await _userManager.FindByIdAsync(idValue!);
        if (user is null)
            throw new UnauthorizedAccessException();
        return await _userService.EnsureEmployerAsync(user);
    }

    private static bool IsValidLogo(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        return LogoExtensions.Contains(ext) && file.Length <= MaxLogoBytes;
    }
}