using ElevateWorkforce.Application.Interfaces;
using ElevateWorkforce.Domain.Entities;
using ElevateWorkforce.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ElevateWorkforce.Application.Services;

public class ApplicationService : IApplicationService
{
    private readonly IApplicationRepository _repository;
    private readonly IJobSeekerRepository _jobSeekerRepository;
    private readonly INotificationRepository _notificationRepository;

    public ApplicationService(
        IApplicationRepository repository,
        IJobSeekerRepository jobSeekerRepository,
        INotificationRepository notificationRepository)
    {
        _repository = repository;
        _jobSeekerRepository = jobSeekerRepository;
        _notificationRepository = notificationRepository;
    }

    public async Task<(IEnumerable<JobApplication> Items, int Total, int TotalPages)> GetForJobSeekerAsync(
        int jobSeekerId, int page, int pageSize)
    {
        var query = _repository.Query()
            .Where(a => a.JobSeekerId == jobSeekerId)
            .OrderByDescending(a => a.CreatedAt);
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total, (int)Math.Ceiling(total / (double)pageSize));
    }

    public async Task<(IEnumerable<JobApplication> Items, int Total, int TotalPages)> GetForCompanyAsync(
        int companyId, int page, int pageSize, string? status)
    {
        var query = _repository.Query().Where(a => a.Job.CompanyId == companyId);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ApplicationStatus>(status, true, out var st))
            query = query.Where(a => a.Status == st);
        query = query.OrderByDescending(a => a.CreatedAt);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total, (int)Math.Ceiling(total / (double)pageSize));
    }

    public Task<JobApplication?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);

    public Task<bool> HasAppliedAsync(int jobSeekerId, int jobId) =>
        _repository.Query().AnyAsync(a => a.JobSeekerId == jobSeekerId && a.JobId == jobId);

    public async Task<JobApplication> CreateAsync(JobApplication application)
    {
        application.Status = ApplicationStatus.Applied;
        var created = await _repository.AddAsync(application);
        created.Timeline.Add(new JobApplicationTimelineEntry
        {
            ApplicationId = created.Id,
            Status = ApplicationStatus.Applied,
            Note = "Application submitted.",
            ChangedByName = created.JobSeeker?.User?.FullName ?? "Applicant"
        });
        await _repository.UpdateAsync(created);

        await _jobSeekerRepository.AddNotificationAsync(new Notification
        {
            JobSeekerId = created.JobSeekerId,
            Title = "Application submitted",
            Message = $"Your application for {created.Job?.Title ?? "the position"} was submitted successfully.",
            Link = $"/applications/details/{created.Id}"
        });

        return created;
    }

    public async Task<JobApplication> UpdateStatusAsync(int applicationId, ApplicationStatus newStatus, string? note, string changedByName)
    {
        var application = await _repository.GetByIdAsync(applicationId)
            ?? throw new KeyNotFoundException($"Application {applicationId} not found.");

        application.Status = newStatus;
        application.ReviewedAt = DateTime.UtcNow;
        if (newStatus == ApplicationStatus.Shortlisted) application.ShortlistedAt = DateTime.UtcNow;
        if (newStatus == ApplicationStatus.Interview) application.InterviewAt = DateTime.UtcNow;

        application.Timeline.Add(new JobApplicationTimelineEntry
        {
            ApplicationId = application.Id,
            Status = newStatus,
            Note = note ?? $"Status changed to {DescribeStatus(newStatus)}.",
            ChangedByName = changedByName
        });

        var notificationTitle = newStatus switch
        {
            ApplicationStatus.Shortlisted => "You have been shortlisted",
            ApplicationStatus.Interview => "You have been invited to an interview",
            ApplicationStatus.Selected => "Congratulations! You have been selected",
            ApplicationStatus.Rejected => "Application update",
            _ => "Application status updated"
        };

        await _jobSeekerRepository.AddNotificationAsync(new Notification
        {
            JobSeekerId = application.JobSeekerId,
            Title = notificationTitle,
            Message = $"Your application for {application.Job?.Title ?? "the position"} is now: {DescribeStatus(newStatus)}.",
            Link = $"/applications/details/{application.Id}"
        });

        await _repository.UpdateAsync(application);
        return application;
    }

    public async Task<bool> WithdrawAsync(int applicationId)
    {
        var application = await _repository.GetByIdAsync(applicationId);
        if (application is null ||
            application.Status == ApplicationStatus.Withdrawn ||
            application.Status == ApplicationStatus.Selected)
            return false;

        application.Status = ApplicationStatus.Withdrawn;
        application.Timeline.Add(new JobApplicationTimelineEntry
        {
            ApplicationId = application.Id,
            Status = ApplicationStatus.Withdrawn,
            Note = "Application withdrawn by the candidate.",
            ChangedByName = application.JobSeeker?.User?.FullName ?? "Candidate"
        });
        await _repository.UpdateAsync(application);
        return true;
    }

    public async Task<int> CountAllAsync() =>
        await _repository.Query().CountAsync();

    public async Task<int> CountForJobSeekerByStatusAsync(int jobSeekerId, ApplicationStatus status) =>
        await _repository.Query().CountAsync(a => a.JobSeekerId == jobSeekerId && a.Status == status);

    public async Task<int> CountForCompanyAsync(int companyId) =>
        await _repository.Query().CountAsync(a => a.Job.CompanyId == companyId);

    public async Task<int> CountForCompanyByStatusAsync(int companyId, ApplicationStatus status) =>
        await _repository.Query().CountAsync(a => a.Job.CompanyId == companyId && a.Status == status);

    public async Task<IEnumerable<JobApplication>> GetRecentForJobSeekerAsync(int jobSeekerId, int take) =>
        await _repository.Query()
            .Where(a => a.JobSeekerId == jobSeekerId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(take)
            .ToListAsync();

    public async Task<IEnumerable<JobApplication>> GetRecentForCompanyAsync(int companyId, int take) =>
        await _repository.Query()
            .Where(a => a.Job.CompanyId == companyId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(take)
            .ToListAsync();

    private static string DescribeStatus(ApplicationStatus status) => status switch
    {
        ApplicationStatus.Applied => "Applied",
        ApplicationStatus.UnderReview => "Under Review",
        ApplicationStatus.Shortlisted => "Shortlisted",
        ApplicationStatus.Interview => "Interview",
        ApplicationStatus.Selected => "Selected",
        ApplicationStatus.Rejected => "Rejected",
        ApplicationStatus.Withdrawn => "Withdrawn",
        _ => status.ToString()
    };
}