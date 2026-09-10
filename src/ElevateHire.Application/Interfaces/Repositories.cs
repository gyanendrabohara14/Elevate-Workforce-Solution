using ElevateHire.Domain.Entities;

namespace ElevateHire.Application.Interfaces;

public interface IJobRepository
{
    Task<Job?> GetByIdAsync(int id, bool includeDeleted = false);
    IQueryable<Job> Query();
    Task<Job> AddAsync(Job job);
    Task UpdateAsync(Job job);
    Task<int> CountAsync();
    Task<int> CountActiveAsync();
    Task<int> CountPendingAsync();
    Task<IEnumerable<Job>> GetRecentAsync(int take);
}

public interface IApplicationRepository
{
    Task<JobApplication?> GetByIdAsync(int id, bool includeDeleted = false);
    IQueryable<JobApplication> Query();
    Task<JobApplication> AddAsync(JobApplication application);
    Task UpdateAsync(JobApplication application);
}

public interface ICompanyRepository
{
    Task<Company?> GetByIdAsync(int id);
    IQueryable<Company> Query();
    Task<Company> AddAsync(Company company);
    Task UpdateAsync(Company company);
    Task<int> CountAsync();
    Task<int> CountPendingAsync();
    Task<IEnumerable<Company>> GetRecentAsync(int take);
}

public interface IJobSeekerRepository
{
    Task<JobSeeker?> GetByIdAsync(int id);
    Task<JobSeeker?> GetByUserIdAsync(int userId);
    Task<JobSeeker?> GetByEmailAsync(string email);
    Task<JobSeeker> AddAsync(JobSeeker jobSeeker);
    Task UpdateAsync(JobSeeker jobSeeker);
    Task AddSavedJobAsync(SavedJob savedJob);
    Task RemoveSavedJobAsync(SavedJob savedJob);
    Task<SavedJob?> GetSavedJobAsync(int jobSeekerId, int jobId);
    Task AddNotificationAsync(Notification notification);
    Task<IEnumerable<Notification>> GetRecentNotificationsAsync(int jobSeekerId, int take);
    Task MarkNotificationsReadAsync(int jobSeekerId);
}

public interface IEmployerRepository
{
    Task<Employer?> GetByIdAsync(int id);
    Task<Employer?> GetByUserIdAsync(int userId);
    Task<Employer> AddAsync(Employer employer);
    Task UpdateAsync(Employer employer);
}

public interface INotificationRepository
{
    IQueryable<Notification> Query();
}

public interface IUserRepository : IJobSeekerRepository
{
    IQueryable<User> QueryUsers();
    IQueryable<SavedJob> QuerySavedJobs();
    Task<User?> FindUserAsync(int id);
    Task<int> SaveChangesAsync();
}