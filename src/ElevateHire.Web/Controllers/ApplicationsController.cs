using ElevateHire.Application.Interfaces;
using ElevateHire.Domain.Entities;
using ElevateHire.Domain.Enums;
using ElevateHire.Infrastructure.Storage;
using ElevateHire.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElevateHire.Web.Controllers;

[Authorize(Roles = "JobSeeker")]
public class ApplicationsController : Controller
{
    private static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx" };
    private const long MaxBytes = 5 * 1024 * 1024;

    private readonly IApplicationService _applicationService;
    private readonly IJobService _jobService;
    private readonly IUserService _userService;
    private readonly IFileStorageService _fileStorage;

    public ApplicationsController(
        IApplicationService applicationService,
        IJobService jobService,
        IUserService userService,
        IFileStorageService fileStorage)
    {
        _applicationService = applicationService;
        _jobService = jobService;
        _userService = userService;
        _fileStorage = fileStorage;
    }

    [HttpGet]
    public async Task<IActionResult> Apply(int id)
    {
        var job = await _jobService.GetByIdAsync(id);
        if (job is null || job.Status == JobStatus.PendingVerification || job.Status == JobStatus.Removed)
            return NotFound();

        var seeker = await CurrentJobSeekerAsync();
        if (seeker is null)
            return NotFound();

        if (!IsOpen(job))
        {
            TempData["error"] = job.IsExpired
                ? "This position has closed. Applications are no longer accepted."
                : "This position is not currently accepting applications.";
            return RedirectToAction("Details", "Jobs", new { id });
        }

        if (await _applicationService.HasAppliedAsync(seeker.Id, id))
        {
            TempData["error"] = "You have already applied for this position.";
            return RedirectToAction("Details", "Jobs", new { id });
        }

        return View(new ApplyViewModel
        {
            JobId = id,
            JobTitle = job.Title,
            CompanyName = job.Company?.Name ?? "Company",
            HasResume = seeker.ResumeStoredName is not null,
            UseUploadedResume = seeker.ResumeStoredName is not null
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(ApplyViewModel model)
    {
        var job = await _jobService.GetByIdAsync(model.JobId);
        if (job is null || !IsOpen(job))
        {
            ModelState.AddModelError(string.Empty, "This position is no longer accepting applications.");
            return View(model);
        }

        var seeker = await CurrentJobSeekerAsync();
        if (seeker is null)
            return NotFound();

        if (await _applicationService.HasAppliedAsync(seeker.Id, job.Id))
        {
            ModelState.AddModelError(string.Empty, "You have already applied for this position.");
            return View(model);
        }

        if (model.Resume is null && seeker.ResumeStoredName is null)
            ModelState.AddModelError(nameof(model.Resume), "Please upload a resume to apply.");

        if (model.Resume is not null)
        {
            if (!IsValidResume(model.Resume))
            {
                ModelState.AddModelError(nameof(model.Resume), "Resume must be a PDF, DOC or DOCX file under 5 MB.");
                return View(model);
            }
        }

        if (!ModelState.IsValid)
            return View(model);

        if (model.Resume is not null)
        {
            await using var stream = model.Resume.OpenReadStream();
            var (storedName, _) = await _fileStorage.SaveAsync(
                stream, model.Resume.FileName, "resumes", AllowedExtensions, MaxBytes);
            await _userService.SetResumeAsync(seeker, storedName, model.Resume.FileName, model.Resume.ContentType ?? "application/octet-stream", model.Resume.Length);
        }

        var application = await _applicationService.CreateAsync(new JobApplication
        {
            JobId = job.Id,
            JobSeekerId = seeker.Id,
            CoverLetter = string.IsNullOrWhiteSpace(model.CoverLetter) ? null : model.CoverLetter.Trim(),
            Status = ApplicationStatus.Applied
        });

        TempData["success"] = "Your application was submitted successfully.";
        return RedirectToAction(nameof(Details), new { id = application.Id });
    }

    [HttpGet]
    public async Task<IActionResult> MyApplications(int page = 1)
    {
        page = Math.Max(1, page);
        var seeker = await CurrentJobSeekerAsync();
        if (seeker is null)
            return NotFound();

        var result = await _applicationService.GetForJobSeekerAsync(seeker.Id, page, 10);

        return View(new ApplicationsDashboardListViewModel
        {
            Page = page,
            PageSize = 10,
            Total = result.Total,
            TotalPages = result.TotalPages,
            Applications = result.Items
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var seeker = await CurrentJobSeekerAsync();
        if (seeker is null)
            return NotFound();

        var application = await _applicationService.GetByIdAsync(id);
        if (application is null || application.JobSeekerId != seeker.Id)
            return NotFound();

        return View(application);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Withdraw(int id)
    {
        var seeker = await CurrentJobSeekerAsync();
        if (seeker is null)
            return NotFound();

        var application = await _applicationService.GetByIdAsync(id);
        if (application is null || application.JobSeekerId != seeker.Id)
            return NotFound();

        await _applicationService.WithdrawAsync(id);
        TempData["success"] = "Your application was withdrawn.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadResume(int id)
    {
        var seeker = await CurrentJobSeekerAsync();
        if (seeker is null)
            return NotFound();

        var application = await _applicationService.GetByIdAsync(id);
        if (application is null || application.JobSeekerId != seeker.Id)
            return NotFound();

        var job = await _jobService.GetByIdAsync(application.JobId);
        if (job is null)
            return NotFound();

        return await ResumeFileAsync(application);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadMyResume()
    {
        var seeker = await CurrentJobSeekerAsync();
        if (seeker is null)
            return NotFound();

        if (seeker.ResumeStoredName is null)
            return NotFound();

        var application = new JobApplication { JobSeeker = seeker };
        return await ResumeFileAsync(application);
    }

    private async Task<IActionResult> ResumeFileAsync(JobApplication application)
    {
        if (application.JobSeeker is null || string.IsNullOrEmpty(application.JobSeeker.ResumeStoredName))
        {
            var seeker = await _userService.GetJobSeekerByIdAsync(application.JobSeekerId);
            if (seeker is null || seeker.ResumeStoredName is null)
                return NotFound();
            application.JobSeeker = seeker;
        }

        var path = _fileStorage.ResolvePath(application.JobSeeker.ResumeStoredName, "resumes");
        if (!System.IO.File.Exists(path))
            return NotFound();

        var bytes = await System.IO.File.ReadAllBytesAsync(path);
        var contentType = application.JobSeeker.ResumeContentType ?? "application/octet-stream";
        var fileName = application.JobSeeker.ResumeFileName ?? "resume.pdf";
        return File(bytes, contentType, fileName);
    }

    private async Task<JobSeeker?> CurrentJobSeekerAsync()
    {
        var idValue = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(idValue, out var userId)) return null;
        return await _userService.GetJobSeekerByUserIdAsync(userId);
    }

    private static bool IsOpen(Job job) =>
        job.Status == JobStatus.Active && !job.IsExpired;

    private static bool IsValidResume(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        return AllowedExtensions.Contains(ext) && file.Length <= MaxBytes;
    }
}