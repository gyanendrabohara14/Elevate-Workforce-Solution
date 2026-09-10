using ElevateHire.Application.Interfaces;
using ElevateHire.Domain.Entities;
using ElevateHire.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ElevateHire.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmployerRepository _employerRepository;
    private readonly ICompanyRepository _companyRepository;

    public UserService(
        IUserRepository userRepository,
        IEmployerRepository employerRepository,
        ICompanyRepository companyRepository)
    {
        _userRepository = userRepository;
        _employerRepository = employerRepository;
        _companyRepository = companyRepository;
    }

    public Task<JobSeeker?> GetJobSeekerByUserIdAsync(int userId) =>
        _userRepository.GetByUserIdAsync(userId);

    public async Task<JobSeeker> EnsureJobSeekerAsync(User user)
    {
        var existing = await _userRepository.GetByUserIdAsync(user.Id);
        if (existing is not null) return existing;

        var jobSeeker = new JobSeeker { UserId = user.Id };
        return await _userRepository.AddAsync(jobSeeker);
    }

    public Task<Employer?> GetEmployerByUserIdAsync(int userId) =>
        _employerRepository.GetByUserIdAsync(userId);

    public async Task<Employer> EnsureEmployerAsync(User user)
    {
        var existing = await _employerRepository.GetByUserIdAsync(user.Id);
        if (existing is not null) return existing;

        var company = new Company
        {
            Name = $"{user.FullName}'s Company",
            Status = CompanyStatus.PendingVerification
        };
        await _companyRepository.AddAsync(company);

        var employer = new Employer { UserId = user.Id, CompanyId = company.Id, Position = "HR" };
        return await _employerRepository.AddAsync(employer);
    }

    public Task UpdateEmployerAsync(Employer employer) =>
        _employerRepository.UpdateAsync(employer);

    public Task<JobSeeker?> GetJobSeekerByIdAsync(int id) =>
        _userRepository.GetByIdAsync(id);

    public Task<Employer?> GetEmployerByIdAsync(int id) =>
        _employerRepository.GetByIdAsync(id);

    public Task UpdateJobSeekerProfileAsync(JobSeeker jobSeeker) =>
        _userRepository.UpdateAsync(jobSeeker);

    public async Task SetResumeAsync(JobSeeker jobSeeker, string storedName, string originalName, string contentType, long size)
    {
        jobSeeker.ResumeStoredName = storedName;
        jobSeeker.ResumeFileName = originalName;
        jobSeeker.ResumeContentType = contentType;
        jobSeeker.ResumeSizeBytes = size;
        jobSeeker.ResumeUploadedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(jobSeeker);
    }

    public async Task ClearResumeAsync(JobSeeker jobSeeker)
    {
        jobSeeker.ResumeStoredName = null;
        jobSeeker.ResumeFileName = null;
        jobSeeker.ResumeContentType = null;
        jobSeeker.ResumeSizeBytes = null;
        jobSeeker.ResumeUploadedAt = null;
        await _userRepository.UpdateAsync(jobSeeker);
    }

    public async Task<bool> ToggleSavedJobAsync(int jobSeekerId, int jobId)
    {
        var existing = await _userRepository.GetSavedJobAsync(jobSeekerId, jobId);
        if (existing is not null)
        {
            await _userRepository.RemoveSavedJobAsync(existing);
            return false;
        }

        await _userRepository.AddSavedJobAsync(new SavedJob { JobSeekerId = jobSeekerId, JobId = jobId });
        return true;
    }

    public async Task<bool> IsJobSavedAsync(int jobSeekerId, int jobId) =>
        await _userRepository.GetSavedJobAsync(jobSeekerId, jobId) is not null;

    public async Task<(IEnumerable<SavedJob> Items, int Total, int TotalPages)> GetSavedJobsAsync(int jobSeekerId, int page, int pageSize)
    {
        var query = _userRepository.QuerySavedJobs()
            .Include(s => s.Job).ThenInclude(j => j.Company)
            .Where(s => s.JobSeekerId == jobSeekerId && !s.Job!.IsDeleted)
            .OrderByDescending(s => s.CreatedAt);
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total, (int)Math.Ceiling(total / (double)pageSize));
    }

    public async Task<(IEnumerable<User> Items, int Total, int TotalPages)> AdminSearchUsersAsync(
        string? keyword, string? role, string? status, int page, int pageSize)
    {
        var query = _userRepository.QueryUsers()
            .Include(u => u.Employer).ThenInclude(e => e!.Company)
            .Include(u => u.JobSeeker)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim().ToLower();
            query = query.Where(u =>
                u.FullName.ToLower().Contains(k) ||
                u.Email!.ToLower().Contains(k));
        }

        if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<UserRole>(role, true, out var r))
            query = query.Where(u => u.Role == r);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var active = status.Equals("active", StringComparison.OrdinalIgnoreCase);
            query = query.Where(u => u.IsActive == active);
        }

        query = query.OrderByDescending(u => u.CreatedAt);
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total, (int)Math.Ceiling(total / (double)pageSize));
    }

    public async Task ToggleUserActiveAsync(int userId)
    {
        var user = await _userRepository.FindUserAsync(userId);
        if (user is null) return;
        user.IsActive = !user.IsActive;
        await _userRepository.SaveChangesAsync();
    }

    public Task<int> CountUsersAsync() => _userRepository.QueryUsers().CountAsync();

    public Task<int> CountByRoleAsync(UserRole role) =>
        _userRepository.QueryUsers().CountAsync(u => u.Role == role);

    public Task NotifyAsync(JobSeeker jobSeeker, string title, string message, string? link) =>
        _userRepository.AddNotificationAsync(new Notification
        {
            JobSeekerId = jobSeeker.Id,
            Title = title,
            Message = message,
            Link = link
        });

    public Task<IEnumerable<Notification>> GetNotificationsAsync(int jobSeekerId, int take) =>
        _userRepository.GetRecentNotificationsAsync(jobSeekerId, take);

    public Task MarkNotificationsReadAsync(int jobSeekerId) =>
        _userRepository.MarkNotificationsReadAsync(jobSeekerId);

    public Task<JobSeeker?> GetJobSeekerByEmailAsync(string email) =>
        _userRepository.GetByEmailAsync(email);
}