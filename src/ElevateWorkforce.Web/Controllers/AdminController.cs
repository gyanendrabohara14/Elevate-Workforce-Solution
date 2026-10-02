using ElevateWorkforce.Application.AI;
using ElevateWorkforce.Application.Interfaces;
using ElevateWorkforce.Domain.Entities;
using ElevateWorkforce.Domain.Enums;
using ElevateWorkforce.Web.Services;
using ElevateWorkforce.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElevateWorkforce.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IUserService _userService;
    private readonly ICompanyService _companyService;
    private readonly IJobService _jobService;
    private readonly IApplicationService _applicationService;
    private readonly IAIService _aiService;

    public AdminController(
        IUserService userService,
        ICompanyService companyService,
        IJobService jobService,
        IApplicationService applicationService,
        IAIService aiService)
    {
        _userService = userService;
        _companyService = companyService;
        _jobService = jobService;
        _applicationService = applicationService;
        _aiService = aiService;
    }

    public async Task<IActionResult> Dashboard()
    {
        var model = new AdminDashboardViewModel
        {
            TotalUsers = await _userService.CountUsersAsync(),
            JobSeekers = await _userService.CountByRoleAsync(UserRole.JobSeeker),
            Employers = await _userService.CountByRoleAsync(UserRole.Employer),
            Companies = await _companyService.CountTotalAsync(),
            PendingCompanies = await _companyService.CountPendingAsync(),
            TotalJobs = await _jobService.CountTotalAsync(),
            PendingJobs = await _jobService.CountByStatusAsync(nameof(JobStatus.PendingVerification)),
            ActiveJobs = await _jobService.CountActiveAsync(),
            Applications = await _applicationService.CountAllAsync(),

            PendingJobsList = (await _jobService.AdminSearchAsync(null, nameof(JobStatus.PendingVerification), 1, 8)).Items,
            RecentCompanies = await _companyService.GetRecentAsync(6),
            RecentUsers = (await _userService.AdminSearchUsersAsync(null, null, null, 1, 8)).Items
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Users(string? keyword, string? role, string? status, int page = 1)
    {
        page = Math.Max(1, page);
        var result = await _userService.AdminSearchUsersAsync(keyword, role, status, page, 10);
        return View(new UserListViewModel
        {
            Keyword = keyword,
            Role = role,
            Status = status,
            Page = page,
            PageSize = 10,
            Total = result.Total,
            TotalPages = result.TotalPages,
            Users = result.Items
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUser(int id)
    {
        await _userService.ToggleUserActiveAsync(id);
        TempData["success"] = "User status updated.";
        return RedirectToAction(nameof(Users));
    }

    [HttpGet]
    public async Task<IActionResult> UserDetails(int id)
    {
        var user = (await _userService.AdminSearchUsersAsync(null, null, null, 1, 1000))
            .Items.FirstOrDefault(u => u.Id == id);
        if (user is null)
            return NotFound();
        return View(user);
    }

    [HttpGet]
    public async Task<IActionResult> Companies(string? keyword, string? status, int page = 1)
    {
        page = Math.Max(1, page);
        var result = await _companyService.AdminSearchAsync(keyword, status, page, 10);
        return View(new CompanyListViewModel
        {
            Keyword = keyword,
            Status = status,
            Page = page,
            PageSize = 10,
            Total = result.Total,
            TotalPages = result.TotalPages,
            Companies = result.Items
        });
    }

    [HttpGet]
    public async Task<IActionResult> CompanyDetails(int id)
    {
        var company = await _companyService.GetByIdAsync(id);
        if (company is null)
            return NotFound();

        var jobs = (await _jobService.GetByCompanyAsync(id, 1, 100)).Items;
        ViewBag.Jobs = jobs;
        return View(company);
    }

    [HttpGet]
    public async Task<IActionResult> Jobs(string? keyword, string? status, int page = 1)
    {
        page = Math.Max(1, page);
        var result = await _jobService.AdminSearchAsync(keyword, status, page, 10);
        return View(new AdminJobsViewModel
        {
            Keyword = keyword,
            Status = status,
            Page = page,
            PageSize = 10,
            Total = result.Total,
            TotalPages = result.TotalPages,
            Jobs = result.Items
        });
    }

    [HttpGet]
    public async Task<IActionResult> JobDetails(int id)
    {
        var job = await _jobService.GetByIdAsync(id);
        if (job is null)
            return NotFound();
        return View(job);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveJob(int id)
    {
        await _jobService.ApproveAsync(id);
        TempData["success"] = "Job approved and published.";
        return RedirectToAction(nameof(JobDetails), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectJob(int id, string? note)
    {
        await _jobService.RejectAsync(id, note);
        TempData["success"] = "Job rejected.";
        return RedirectToAction(nameof(JobDetails), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveJob(int id)
    {
        await _jobService.SoftDeleteAsync(id);
        TempData["success"] = "Job removed.";
        return RedirectToAction(nameof(Jobs));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnpublishJob(int id)
    {
        await _jobService.UnpublishAsync(id);
        TempData["success"] = "Job unpublished.";
        return RedirectToAction(nameof(JobDetails), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreJob(int id)
    {
        await _jobService.RestoreAsync(id);
        TempData["success"] = "Job restored (pending verification).";
        return RedirectToAction(nameof(Jobs));
    }

    public async Task<IActionResult> Applications(string? status, int page = 1)
    {
        page = Math.Max(1, page);

        var all = (await _applicationService.GetForCompanyAsync(1, 1, 1, null)).Items.FirstOrDefault() != null;

        var companies = await _companyService.AdminSearchAsync(null, null, 1, 100);
        var firstCompanyId = companies.Items.FirstOrDefault()?.Id ?? 0;

        ApplicationsDashboardListViewModel model = new();
        if (firstCompanyId > 0)
        {
            var result = await _applicationService.GetForCompanyAsync(firstCompanyId, page, 10, status);
            model.Status = status;
            model.Page = page;
            model.PageSize = 10;
            model.Total = result.Total;
            model.TotalPages = result.TotalPages;
            model.Applications = result.Items;
        }
        else
        {
            model.Applications = Enumerable.Empty<JobApplication>();
            model.TotalPages = 1;
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveCompany(int id)
    {
        await _companyService.UpdateStatusAsync(id, CompanyStatus.Active);
        TempData["success"] = "Company approved and verified.";
        return RedirectToAction(nameof(CompanyDetails), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectCompany(int id)
    {
        await _companyService.UpdateStatusAsync(id, CompanyStatus.Rejected);
        TempData["success"] = "Company rejected.";
        return RedirectToAction(nameof(CompanyDetails), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResumeCompany(int id)
    {
        await _companyService.UpdateStatusAsync(id, CompanyStatus.PendingVerification);
        TempData["success"] = "Company returned to pending verification.";
        return RedirectToAction(nameof(CompanyDetails), new { id });
    }

    public IActionResult Settings()
    {
        ViewBag.AIEnabled = _aiService.IsEnabled;
        return View();
    }
}