using ElevateHire.Application.Interfaces;
using ElevateHire.Domain.Entities;
using ElevateHire.Domain.Enums;
using ElevateHire.Infrastructure.Storage;
using ElevateHire.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElevateHire.Web.Controllers;

[Authorize(Roles = "JobSeeker")]
public class JobSeekerController : Controller
{
    private static readonly string[] ResumeExtensions = { ".pdf", ".doc", ".docx" };
    private const long MaxResumeBytes = 5 * 1024 * 1024;

    private readonly UserManager<User> _userManager;
    private readonly IUserService _userService;
    private readonly IJobService _jobService;
    private readonly IApplicationService _applicationService;
    private readonly IFileStorageService _fileStorage;

    public JobSeekerController(
        UserManager<User> userManager,
        IUserService userService,
        IJobService jobService,
        IApplicationService applicationService,
        IFileStorageService fileStorage)
    {
        _userManager = userManager;
        _userService = userService;
        _jobService = jobService;
        _applicationService = applicationService;
        _fileStorage = fileStorage;
    }

    public async Task<IActionResult> Dashboard()
    {
        var seeker = await CurrentJobSeekerAsync();
        if (seeker is null)
            return NotFound();

        var model = new JobSeekerDashboardViewModel
        {
            Profile = seeker,
            TotalApplications = await _applicationService.CountForJobSeekerByStatusAsync(seeker.Id, ApplicationStatus.Applied),
            UnderReview = await _applicationService.CountForJobSeekerByStatusAsync(seeker.Id, ApplicationStatus.UnderReview),
            Shortlisted = await _applicationService.CountForJobSeekerByStatusAsync(seeker.Id, ApplicationStatus.Shortlisted),
            Interviews = await _applicationService.CountForJobSeekerByStatusAsync(seeker.Id, ApplicationStatus.Interview),
            SavedJobs = (await _userService.GetSavedJobsAsync(seeker.Id, 1, 100)).Total,
            ProfileCompletion = seeker.ProfileCompletionPercent(),
            RecentApplications = (await _applicationService.GetRecentForJobSeekerAsync(seeker.Id, 5)).ToList(),
            RecommendedJobs = (await _jobService.GetRecommendedAsync(seeker.Id, 4)).ToList(),
            SavedJobItems = (await _userService.GetSavedJobsAsync(seeker.Id, 1, 5)).Items.ToList(),
            Notifications = (await _userService.GetNotificationsAsync(seeker.Id, 6)).ToList()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Profile(string? firstTime)
    {
        var seeker = await CurrentJobSeekerAsync();
        if (seeker is null)
            return NotFound();

        ViewBag.FirstTime = firstTime == "true";
        var user = await _userManager.FindByIdAsync(seeker.UserId.ToString());

        return View(new JobSeekerProfileViewModel
        {
            FullName = user?.FullName ?? string.Empty,
            ProfessionalTitle = seeker.ProfessionalTitle,
            Location = seeker.Location,
            Phone = seeker.Phone,
            About = seeker.About,
            Skills = seeker.Skills,
            Education = seeker.Education,
            Experience = seeker.Experience,
            Certifications = seeker.Certifications,
            Languages = seeker.Languages
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(JobSeekerProfileViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var seeker = await CurrentJobSeekerAsync();
        var user = await _userManager.FindByIdAsync(seeker.UserId.ToString());

        if (user is not null && !string.IsNullOrWhiteSpace(model.FullName) && user.FullName != model.FullName.Trim())
        {
            user.FullName = model.FullName.Trim();
            await _userManager.UpdateAsync(user);
        }

        seeker.ProfessionalTitle = model.ProfessionalTitle?.Trim();
        seeker.Location = model.Location?.Trim();
        seeker.Phone = model.Phone?.Trim();
        seeker.About = model.About?.Trim();
        seeker.Skills = model.Skills?.Trim();
        seeker.Education = model.Education?.Trim();
        seeker.Experience = model.Experience?.Trim();
        seeker.Certifications = model.Certifications?.Trim();
        seeker.Languages = model.Languages?.Trim();

        if (model.Resume is not null)
        {
            if (!IsValidResume(model.Resume))
            {
                ModelState.AddModelError(nameof(model.Resume), "Resume must be a PDF, DOC or DOCX file under 5 MB.");
                return View(model);
            }

            await using var stream = model.Resume.OpenReadStream();
            var (storedName, _) = await _fileStorage.SaveAsync(
                stream, model.Resume.FileName, "resumes", ResumeExtensions, MaxResumeBytes);
            await _userService.SetResumeAsync(seeker, storedName, model.Resume.FileName, model.Resume.ContentType ?? "application/octet-stream", model.Resume.Length);
        }

        await _userService.UpdateJobSeekerProfileAsync(seeker);

        TempData["success"] = "Your profile was updated.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveResume()
    {
        var seeker = await CurrentJobSeekerAsync();
        await _userService.ClearResumeAsync(seeker);
        TempData["success"] = "Resume removed.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    public async Task<IActionResult> Applications(int page = 1)
    {
        var seeker = await CurrentJobSeekerAsync();
        page = Math.Max(1, page);

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
    public async Task<IActionResult> SavedJobs(int page = 1)
    {
        var seeker = await CurrentJobSeekerAsync();
        page = Math.Max(1, page);

        var result = await _userService.GetSavedJobsAsync(seeker.Id, page, 10);
        return View(new SavedJobsViewModel
        {
            Items = result.Items.Select(s => s.Job).ToList(),
            Page = page,
            PageSize = 10,
            Total = result.Total,
            TotalPages = result.TotalPages
        });
    }

    [HttpGet]
    public async Task<IActionResult> Notifications()
    {
        var seeker = await CurrentJobSeekerAsync();
        var notifications = (await _userService.GetNotificationsAsync(seeker.Id, 50)).ToList();
        await _userService.MarkNotificationsReadAsync(seeker.Id);
        return View(notifications);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadResume()
    {
        var seeker = await CurrentJobSeekerAsync();
        if (seeker is null || seeker.ResumeStoredName is null)
            return NotFound();

        var path = _fileStorage.ResolvePath(seeker.ResumeStoredName, "resumes");
        if (!System.IO.File.Exists(path))
            return NotFound();

        return File(
            await System.IO.File.ReadAllBytesAsync(path),
            seeker.ResumeContentType ?? "application/octet-stream",
            seeker.ResumeFileName ?? "resume.pdf");
    }

    private async Task<JobSeeker> CurrentJobSeekerAsync()
    {
        var idValue = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(idValue) || !int.TryParse(idValue, out _))
            throw new UnauthorizedAccessException();

        var user = await _userManager.FindByIdAsync(idValue!);
        if (user is null)
            throw new UnauthorizedAccessException();
        return await _userService.EnsureJobSeekerAsync(user);
    }

    private static bool IsValidResume(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        return ResumeExtensions.Contains(ext) && file.Length <= MaxResumeBytes;
    }
}