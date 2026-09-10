using ElevateHire.Application.Interfaces;
using ElevateHire.Domain.Enums;
using ElevateHire.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElevateHire.Web.Controllers;

public class HomeController : Controller
{
    private readonly IJobService _jobService;
    private readonly ICompanyService _companyService;
    private readonly IUserService _userService;
    private readonly IApplicationService _applicationService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        IJobService jobService,
        ICompanyService companyService,
        IUserService userService,
        IApplicationService applicationService,
        ILogger<HomeController> logger)
    {
        _jobService = jobService;
        _companyService = companyService;
        _userService = userService;
        _applicationService = applicationService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var featured = await _jobService.SearchAsync(null, null, null, null, null, null, null, null, null, "newest", 1, 6);

        var categories = Enum.GetValues<JobCategory>()
            .Select(c => c.ToString())
            .ToList();

        var viewModel = new HomeViewModel
        {
            Stats = new HomeStatsViewModel
            {
                ActiveJobs = await _jobService.CountActiveAsync(),
                Companies = await _companyService.CountTotalAsync(),
                JobSeekers = await _userService.CountByRoleAsync(UserRole.JobSeeker),
                Applications = await _applicationService.CountAllAsync()
            },
            FeaturedJobs = featured.Items,
            Categories = categories
        };

        return View(viewModel);
    }

    public IActionResult Privacy() => View();

    [Route("/error")]
    public IActionResult Error() => View();
}