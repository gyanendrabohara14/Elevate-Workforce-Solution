using ElevateHire.Application.Interfaces;
using ElevateHire.Domain.Entities;
using ElevateHire.Domain.Enums;
using ElevateHire.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElevateHire.Web.Controllers;

public class JobsController : Controller
{
    private readonly IJobService _jobService;
    private readonly IUserService _userService;
    private readonly IApplicationService _applicationService;

    public JobsController(IJobService jobService, IUserService userService, IApplicationService applicationService)
    {
        _jobService = jobService;
        _userService = userService;
        _applicationService = applicationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? keyword, string? location, string? category, string? jobType, string? experience,
        decimal? minSalary, decimal? maxSalary, string? posted, bool? remote, string? sort, int page = 1)
    {
        page = Math.Max(1, page);
        var result = await _jobService.SearchAsync(
            keyword, location, category, jobType, experience, minSalary, maxSalary, posted, remote, sort, page, 9);

        var saved = await SavedIdsAsync(result.Items.Select(j => j.Id).ToList());

        return View(new JobSearchViewModel
        {
            Keyword = keyword,
            Location = location,
            Category = category,
            JobType = jobType,
            Experience = experience,
            MinSalary = minSalary,
            MaxSalary = maxSalary,
            Posted = posted,
            Remote = remote,
            Sort = sort,
            Page = page,
            PageSize = 9,
            Total = result.Total,
            TotalPages = result.TotalPages,
            Jobs = result.Items,
            SavedIds = saved
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var job = await _jobService.GetByIdAsync(id);
        if (job is null)
            return NotFound();

        if (job.Status == JobStatus.PendingVerification || job.Status == JobStatus.Removed)
            return NotFound();

        var seeker = await CurrentJobSeekerAsync();
        var saved = seeker is not null && await _userService.IsJobSavedAsync(seeker.Id, id);
        var hasApplied = seeker is not null && await _applicationService.HasAppliedAsync(seeker.Id, id);

        return View(new JobDetailsViewModel { Job = job, IsSaved = saved, HasApplied = hasApplied });
    }

    [HttpPost]
    [Authorize(Roles = "JobSeeker")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSave(int id)
    {
        var seeker = await CurrentJobSeekerAsync();
        if (seeker is null)
            return Json(new { success = false, message = "No job seeker profile found." });

        var nowSaved = await _userService.ToggleSavedJobAsync(seeker.Id, id);
        return Json(new { success = true, saved = nowSaved });
    }

    private async Task<JobSeeker?> CurrentJobSeekerAsync()
    {
        var userIdValue = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (User.Identity?.IsAuthenticated != true || !int.TryParse(userIdValue, out var userId))
            return null;
        return await _userService.GetJobSeekerByUserIdAsync(userId);
    }

    private async Task<IDictionary<int, bool>> SavedIdsAsync(List<int> jobIds)
    {
        var dict = new Dictionary<int, bool>();
        var seeker = await CurrentJobSeekerAsync();
        if (seeker is null) return dict;
        foreach (var id in jobIds)
            dict[id] = await _userService.IsJobSavedAsync(seeker.Id, id);
        return dict;
    }
}