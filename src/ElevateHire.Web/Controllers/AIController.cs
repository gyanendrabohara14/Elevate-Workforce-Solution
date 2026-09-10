using ElevateHire.Application.AI;
using ElevateHire.Application.Interfaces;
using ElevateHire.Domain.Entities;
using ElevateHire.Infrastructure.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElevateHire.Web.Controllers;

public class AIController : Controller
{
    private readonly IAIService _aiService;
    private readonly IUserService _userService;
    private readonly IJobService _jobService;
    private readonly IFileStorageService _fileStorage;

    public AIController(IAIService aiService, IUserService userService, IJobService jobService, IFileStorageService fileStorage)
    {
        _aiService = aiService;
        _userService = userService;
        _jobService = jobService;
        _fileStorage = fileStorage;
    }

    [HttpPost]
    [Authorize(Roles = "Employer,Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateJobDescription(
        string position, string experience, string skills, string location, string jobType, string? companyName, string? notes)
    {
        var result = await _aiService.GenerateJobDescriptionAsync(new JobDescriptionRequest
        {
            Position = position,
            Experience = experience,
            Skills = skills,
            Location = location,
            JobType = jobType,
            CompanyName = companyName,
            AdditionalNotes = notes
        });

        return Json(new
        {
            success = result.Success && !string.IsNullOrWhiteSpace(result.Description),
            error = result.Error,
            description = result.Description,
            responsibilities = result.Responsibilities,
            requirements = result.Requirements,
            qualifications = result.Qualifications,
            skills = result.Skills,
            benefits = result.Benefits
        });
    }

    [HttpPost]
    [Authorize(Roles = "JobSeeker")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateCoverLetter(int jobId)
    {
        var userIdValue = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var seeker = await _userService.GetJobSeekerByUserIdAsync(userId);
        var job = await _jobService.GetByIdAsync(jobId);
        if (seeker is null || job is null)
            return NotFound();

        var profile = string.Join(" | ",
            seeker.ProfessionalTitle, seeker.Location, seeker.About, seeker.Skills, seeker.Education, seeker.Experience);
        var jobDescription = $"{job.Title} at {job.Company?.Name} | {job.Description} | {job.Responsibilities} | {job.Requirements}";

        var letter = await _aiService.GenerateCoverLetterAsync(profile, jobDescription);
        if (string.IsNullOrWhiteSpace(letter))
            return Json(new { success = false, error = "AI assistance is temporarily unavailable. You can continue manually." });

        return Json(new { success = true, coverLetter = letter });
    }

    [HttpGet]
    public async Task<IActionResult> Candidate(int id)
    {
        // Placeholder action referenced by shared views; real logic lives in EmployerController.
        return RedirectToAction("Applicant", "Employer", new { id });
    }
}