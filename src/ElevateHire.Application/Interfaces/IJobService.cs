using ElevateHire.Domain.Entities;

namespace ElevateHire.Application.Interfaces;

public interface IJobService
{
    Task<Job?> GetByIdAsync(int id);
    Task<(IEnumerable<Job> Items, int Total, int TotalPages)> SearchAsync(string? keyword, string? location, string? category, string? jobType, string? experience, decimal? minSalary, decimal? maxSalary, string? posted, bool? remote, string? sort, int page, int pageSize);
    Task<(IEnumerable<Job> Items, int Total, int TotalPages)> GetByCompanyAsync(int companyId, int page, int pageSize);
    Task<Job> CreateAsync(Job job);
    Task<Job> UpdateAsync(Job job);
    Task<bool> DeleteAsync(int id);
    Task<bool> SoftDeleteAsync(int id);
    Task ApproveAsync(int id);
    Task RejectAsync(int id, string? reason);
    Task UnpublishAsync(int id);
    Task RestoreAsync(int id);
    Task<(IEnumerable<Job> Items, int Total, int TotalPages)> AdminSearchAsync(string? keyword, string? status, int page, int pageSize);
    Task<IEnumerable<Job>> GetRecommendedAsync(int jobSeekerId, int take);
    Task<bool> IsOwnedByCompanyAsync(int jobId, int companyId);
    Task<int> CountByStatusAsync(string status);
    Task<int> CountActiveAsync();
    Task<int> CountTotalAsync();
}
