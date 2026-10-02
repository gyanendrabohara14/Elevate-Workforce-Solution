using ElevateWorkforce.Application.Interfaces;
using ElevateWorkforce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ElevateWorkforce.Infrastructure.Data;

namespace ElevateWorkforce.Infrastructure.Repositories;

public class JobRepository : IJobRepository
{
    private readonly ApplicationDbContext _context;

    public JobRepository(ApplicationDbContext context) => _context = context;

    public IQueryable<Job> Query() =>
        _context.Jobs
            .Include(j => j.Company)
            .Include(j => j.Applications)
            .Where(j => !j.IsDeleted);

    public async Task<Job?> GetByIdAsync(int id, bool includeDeleted = false)
    {
        var query = includeDeleted ? _context.Jobs : Query();
        return await query.FirstOrDefaultAsync(j => j.Id == id);
    }

    public async Task<Job> AddAsync(Job job)
    {
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();
        return job;
    }

    public Task UpdateAsync(Job job)
    {
        job.UpdatedAt = DateTime.UtcNow;
        _context.Jobs.Update(job);
        return _context.SaveChangesAsync();
    }

    public Task<int> CountAsync() => _context.Jobs.CountAsync(j => !j.IsDeleted);

    public Task<int> CountActiveAsync() => _context.Jobs.CountAsync(j => !j.IsDeleted && j.Status == Domain.Enums.JobStatus.Active);

    public Task<int> CountPendingAsync() => _context.Jobs.CountAsync(j => !j.IsDeleted && j.Status == Domain.Enums.JobStatus.PendingVerification);

    public async Task<IEnumerable<Job>> GetRecentAsync(int take) =>
        await Query()
            .OrderByDescending(j => j.CreatedAt)
            .Take(take)
            .ToListAsync();
}

public class ApplicationRepository : IApplicationRepository
{
    private readonly ApplicationDbContext _context;

    public ApplicationRepository(ApplicationDbContext context) => _context = context;

    public IQueryable<JobApplication> Query() =>
        _context.Applications
            .Include(a => a.Job).ThenInclude(j => j.Company)
            .Include(a => a.JobSeeker).ThenInclude(js => js.User)
            .Include(a => a.Timeline)
            .Where(a => !a.IsDeleted);

    public async Task<JobApplication?> GetByIdAsync(int id, bool includeDeleted = false)
    {
        var query = includeDeleted ? _context.Applications : Query();
        return await query.FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<JobApplication> AddAsync(JobApplication application)
    {
        _context.Applications.Add(application);
        await _context.SaveChangesAsync();
        return application;
    }

    public Task UpdateAsync(JobApplication application)
    {
        application.UpdatedAt = DateTime.UtcNow;
        _context.Applications.Update(application);
        return _context.SaveChangesAsync();
    }
}

public class CompanyRepository : ICompanyRepository
{
    private readonly ApplicationDbContext _context;

    public CompanyRepository(ApplicationDbContext context) => _context = context;

    public IQueryable<Company> Query() =>
        _context.Companies
            .Include(c => c.Employers).ThenInclude(e => e.User)
            .Include(c => c.Jobs)
            .Where(c => !c.IsDeleted);

    public async Task<Company?> GetByIdAsync(int id) =>
        await Query().FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Company> AddAsync(Company company)
    {
        _context.Companies.Add(company);
        await _context.SaveChangesAsync();
        return company;
    }

    public Task UpdateAsync(Company company)
    {
        company.UpdatedAt = DateTime.UtcNow;
        _context.Companies.Update(company);
        return _context.SaveChangesAsync();
    }

    public Task<int> CountAsync() => _context.Companies.CountAsync(c => !c.IsDeleted);

    public Task<int> CountPendingAsync() => _context.Companies.CountAsync(c => !c.IsDeleted && c.Status == Domain.Enums.CompanyStatus.PendingVerification);

    public async Task<IEnumerable<Company>> GetRecentAsync(int take) =>
        await Query().OrderByDescending(c => c.CreatedAt).Take(take).ToListAsync();
}

public class JobSeekerRepository : IJobSeekerRepository
{
    private readonly ApplicationDbContext _context;

    public JobSeekerRepository(ApplicationDbContext context) => _context = context;

    private IQueryable<JobSeeker> BaseQuery() =>
        _context.JobSeekers.Include(js => js.User).Where(js => !js.IsDeleted);

    public Task<JobSeeker?> GetByIdAsync(int id) =>
        BaseQuery().FirstOrDefaultAsync(js => js.Id == id);

    public Task<JobSeeker?> GetByUserIdAsync(int userId) =>
        BaseQuery().FirstOrDefaultAsync(js => js.UserId == userId);

    public Task<JobSeeker?> GetByEmailAsync(string email) =>
        BaseQuery().FirstOrDefaultAsync(js => js.User.Email == email);

    public async Task<JobSeeker> AddAsync(JobSeeker jobSeeker)
    {
        _context.JobSeekers.Add(jobSeeker);
        await _context.SaveChangesAsync();
        return jobSeeker;
    }

    public Task UpdateAsync(JobSeeker jobSeeker)
    {
        jobSeeker.UpdatedAt = DateTime.UtcNow;
        _context.JobSeekers.Update(jobSeeker);
        return _context.SaveChangesAsync();
    }

    public Task AddSavedJobAsync(SavedJob savedJob)
    {
        _context.SavedJobs.Add(savedJob);
        return _context.SaveChangesAsync();
    }

    public Task RemoveSavedJobAsync(SavedJob savedJob)
    {
        _context.SavedJobs.Remove(savedJob);
        return _context.SaveChangesAsync();
    }

    public Task<SavedJob?> GetSavedJobAsync(int jobSeekerId, int jobId) =>
        _context.SavedJobs.FirstOrDefaultAsync(s => s.JobSeekerId == jobSeekerId && s.JobId == jobId);

    public Task AddNotificationAsync(Notification notification)
    {
        _context.Notifications.Add(notification);
        return _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Notification>> GetRecentNotificationsAsync(int jobSeekerId, int take) =>
        await _context.Notifications
            .Where(n => n.JobSeekerId == jobSeekerId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .ToListAsync();

    public Task MarkNotificationsReadAsync(int jobSeekerId) =>
        _context.Notifications
            .Where(n => n.JobSeekerId == jobSeekerId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, DateTime.UtcNow));
}

public class EmployerRepository : IEmployerRepository
{
    private readonly ApplicationDbContext _context;

    public EmployerRepository(ApplicationDbContext context) => _context = context;

    private IQueryable<Employer> BaseQuery() =>
        _context.Employers.Include(e => e.User).Include(e => e.Company).Where(e => !e.IsDeleted);

    public Task<Employer?> GetByIdAsync(int id) =>
        BaseQuery().FirstOrDefaultAsync(e => e.Id == id);

    public Task<Employer?> GetByUserIdAsync(int userId) =>
        BaseQuery().FirstOrDefaultAsync(e => e.UserId == userId);

    public async Task<Employer> AddAsync(Employer employer)
    {
        _context.Employers.Add(employer);
        await _context.SaveChangesAsync();
        return employer;
    }

    public Task UpdateAsync(Employer employer)
    {
        employer.UpdatedAt = DateTime.UtcNow;
        _context.Employers.Update(employer);
        return _context.SaveChangesAsync();
    }
}

public class NotificationRepository : INotificationRepository
{
    private readonly ApplicationDbContext _context;

    public NotificationRepository(ApplicationDbContext context) => _context = context;

    public IQueryable<Notification> Query() =>
        _context.Notifications.Where(n => !n.IsDeleted);
}

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;
    private readonly JobSeekerRepository _jobSeekerRepository;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
        _jobSeekerRepository = new JobSeekerRepository(context);
    }

    public IQueryable<User> QueryUsers() => _context.Users.AsQueryable();
    public IQueryable<SavedJob> QuerySavedJobs() => _context.SavedJobs.AsQueryable();

    public Task<User?> FindUserAsync(int id) => _context.Users.FindAsync(id).AsTask();

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();

    public Task<JobSeeker?> GetByIdAsync(int id) => _jobSeekerRepository.GetByIdAsync(id);
    public Task<JobSeeker?> GetByUserIdAsync(int userId) => _jobSeekerRepository.GetByUserIdAsync(userId);
    public Task<JobSeeker?> GetByEmailAsync(string email) => _jobSeekerRepository.GetByEmailAsync(email);
    public Task<JobSeeker> AddAsync(JobSeeker jobSeeker) => _jobSeekerRepository.AddAsync(jobSeeker);
    public Task UpdateAsync(JobSeeker jobSeeker) => _jobSeekerRepository.UpdateAsync(jobSeeker);
    public Task AddSavedJobAsync(SavedJob savedJob) => _jobSeekerRepository.AddSavedJobAsync(savedJob);
    public Task RemoveSavedJobAsync(SavedJob savedJob) => _jobSeekerRepository.RemoveSavedJobAsync(savedJob);
    public Task<SavedJob?> GetSavedJobAsync(int jobSeekerId, int jobId) => _jobSeekerRepository.GetSavedJobAsync(jobSeekerId, jobId);
    public Task AddNotificationAsync(Notification notification) => _jobSeekerRepository.AddNotificationAsync(notification);
    public Task<IEnumerable<Notification>> GetRecentNotificationsAsync(int jobSeekerId, int take) => _jobSeekerRepository.GetRecentNotificationsAsync(jobSeekerId, take);
    public Task MarkNotificationsReadAsync(int jobSeekerId) => _jobSeekerRepository.MarkNotificationsReadAsync(jobSeekerId);
}