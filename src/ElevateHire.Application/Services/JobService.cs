using ElevateWorkforce.Application.Interfaces;
using ElevateWorkforce.Domain.Entities;
using ElevateWorkforce.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ElevateWorkforce.Application.Services;

public class JobService : IJobService
{
    private readonly IJobRepository _repository;

    public JobService(IJobRepository repository) => _repository = repository;

    public Task<Job?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);

    public async Task<(IEnumerable<Job> Items, int Total, int TotalPages)> SearchAsync(
        string? keyword, string? location, string? category, string? jobType, string? experience,
        decimal? minSalary, decimal? maxSalary, string? posted, bool? remote, string? sort,
        int page, int pageSize)
    {
        var query = _repository.Query()
            .Where(j => j.Status == JobStatus.Active && (j.Deadline == null || j.Deadline > DateTime.UtcNow));

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim().ToLower();
            query = query.Where(j =>
                j.Title.ToLower().Contains(k) ||
                j.Skills!.ToLower().Contains(k) ||
                j.Description!.ToLower().Contains(k) ||
                j.Company.Name.ToLower().Contains(k));
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            var loc = location.Trim().ToLower();
            query = query.Where(j => j.Location!.ToLower().Contains(loc) || j.IsRemote);
        }

        if (!string.IsNullOrWhiteSpace(category) && Enum.TryParse<JobCategory>(category, true, out var cat))
            query = query.Where(j => j.Category == cat);

        if (!string.IsNullOrWhiteSpace(jobType) && Enum.TryParse<JobType>(jobType, true, out var jt))
            query = query.Where(j => j.JobType == jt);

        if (!string.IsNullOrWhiteSpace(experience) && Enum.TryParse<ExperienceLevel>(experience, true, out var exp))
            query = query.Where(j => j.ExperienceLevel == exp);

        if (minSalary.HasValue)
            query = query.Where(j => j.MaxSalary == null || j.MaxSalary >= minSalary);

        if (maxSalary.HasValue)
            query = query.Where(j => j.MinSalary == null || j.MinSalary <= maxSalary);

        if (remote.HasValue)
            query = query.Where(j => j.IsRemote);

        if (!string.IsNullOrWhiteSpace(posted))
        {
            var days = posted.ToLowerInvariant() switch
            {
                "today" => 1,
                "week" => 7,
                "month" => 30,
                _ => 0
            };
            if (days > 0)
            {
                var since = DateTime.UtcNow.AddDays(-days);
                query = query.Where(j => j.PublishedAt >= since);
            }
        }

        query = sort?.ToLowerInvariant() switch
        {
            "salary" => query.OrderByDescending(j => j.MaxSalary),
            "relevance" => query.OrderByDescending(j => j.CreatedAt),
            _ => query.OrderByDescending(j => j.PublishedAt)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        if (totalPages == 0) totalPages = 1;

        return (items, total, totalPages);
    }

    public async Task<(IEnumerable<Job> Items, int Total, int TotalPages)> GetByCompanyAsync(int companyId, int page, int pageSize)
    {
        var query = _repository.Query().Where(j => j.CompanyId == companyId)
            .OrderByDescending(j => j.CreatedAt);
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total, (int)Math.Ceiling(total / (double)pageSize));
    }

    public Task<Job> CreateAsync(Job job)
    {
        return _repository.AddAsync(job);
    }

    public async Task<Job> UpdateAsync(Job job)
    {
        await _repository.UpdateAsync(job);
        return job;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var job = await _repository.GetByIdAsync(id);
        if (job is null) return false;
        await _repository.UpdateAsync(job);
        return true;
    }

    public async Task<bool> SoftDeleteAsync(int id)
    {
        var job = await _repository.GetByIdAsync(id);
        if (job is null) return false;
        job.IsDeleted = true;
        await _repository.UpdateAsync(job);
        return true;
    }

    public async Task ApproveAsync(int id)
    {
        var job = await RepoRequireAsync(id);
        job.Status = JobStatus.Active;
        job.PublishedAt = DateTime.UtcNow;
        if (job.IsExpired) job.Status = JobStatus.Expired;
        await _repository.UpdateAsync(job);
    }

    public async Task RejectAsync(int id, string? reason)
    {
        var job = await RepoRequireAsync(id);
        job.Status = JobStatus.Rejected;
        await _repository.UpdateAsync(job);
    }

    public async Task UnpublishAsync(int id)
    {
        var job = await RepoRequireAsync(id);
        job.Status = JobStatus.Closed;
        await _repository.UpdateAsync(job);
    }

    public async Task RestoreAsync(int id)
    {
        var job = await _repository.GetByIdAsync(id, includeDeleted: true);
        if (job is null) return;
        job.IsDeleted = false;
        job.Status = JobStatus.PendingVerification;
        await _repository.UpdateAsync(job);
    }

    public async Task<(IEnumerable<Job> Items, int Total, int TotalPages)> AdminSearchAsync(
        string? keyword, string? status, int page, int pageSize)
    {
        var query = _repository.Query().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim().ToLower();
            query = query.Where(j =>
                j.Title.ToLower().Contains(k) ||
                j.Company.Name.ToLower().Contains(k) ||
                j.Location!.ToLower().Contains(k));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<JobStatus>(status, true, out var st))
            query = query.Where(j => j.Status == st);

        query = query.OrderByDescending(j => j.CreatedAt);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total, (int)Math.Ceiling(total / (double)pageSize));
    }

    public async Task<IEnumerable<Job>> GetRecommendedAsync(int jobSeekerId, int take) =>
        await _repository.Query()
            .Where(j => j.Status == JobStatus.Active && (j.Deadline == null || j.Deadline > DateTime.UtcNow)
                && j.Applications.All(a => a.JobSeekerId != jobSeekerId))
            .OrderByDescending(j => j.CreatedAt)
            .Take(take)
            .ToListAsync();

    public async Task<bool> IsOwnedByCompanyAsync(int jobId, int companyId)
    {
        var job = await _repository.GetByIdAsync(jobId);
        return job is not null && job.CompanyId == companyId;
    }

    public Task<int> CountByStatusAsync(string status)
    {
        return Enum.TryParse<JobStatus>(status, true, out var st)
            ? _repository.Query().CountAsync(j => j.Status == st)
            : Task.FromResult(0);
    }

    public Task<int> CountActiveAsync() => _repository.CountActiveAsync();
    public Task<int> CountTotalAsync() => _repository.CountAsync();

    private async Task<Job> RepoRequireAsync(int id)
    {
        var job = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Job {id} not found.");
        return job;
    }
}