using ElevateHire.Domain.Entities;
using ElevateHire.Domain.Enums;

namespace ElevateHire.Application.Interfaces;

public interface IUserService
{
    Task<JobSeeker?> GetJobSeekerByUserIdAsync(int userId);
    Task<JobSeeker> EnsureJobSeekerAsync(User user);
    Task<Employer?> GetEmployerByUserIdAsync(int userId);
    Task<Employer> EnsureEmployerAsync(User user);
    Task UpdateEmployerAsync(Employer employer);
    Task<JobSeeker?> GetJobSeekerByIdAsync(int id);
    Task<Employer?> GetEmployerByIdAsync(int id);
    Task UpdateJobSeekerProfileAsync(JobSeeker jobSeeker);
    Task SetResumeAsync(JobSeeker jobSeeker, string storedName, string originalName, string contentType, long size);
    Task ClearResumeAsync(JobSeeker jobSeeker);
    Task<bool> ToggleSavedJobAsync(int jobSeekerId, int jobId);
    Task<bool> IsJobSavedAsync(int jobSeekerId, int jobId);
    Task<(IEnumerable<SavedJob> Items, int Total, int TotalPages)> GetSavedJobsAsync(int jobSeekerId, int page, int pageSize);
    Task<(IEnumerable<User> Items, int Total, int TotalPages)> AdminSearchUsersAsync(string? keyword, string? role, string? status, int page, int pageSize);
    Task ToggleUserActiveAsync(int userId);
    Task<int> CountUsersAsync();
    Task<int> CountByRoleAsync(UserRole role);
    Task NotifyAsync(JobSeeker jobSeeker, string title, string message, string? link);
    Task<IEnumerable<Notification>> GetNotificationsAsync(int jobSeekerId, int take);
    Task MarkNotificationsReadAsync(int jobSeekerId);
    Task<JobSeeker?> GetJobSeekerByEmailAsync(string email);
}
