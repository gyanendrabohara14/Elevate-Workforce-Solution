using ElevateWorkforce.Domain.Entities;
using ElevateWorkforce.Domain.Enums;

namespace ElevateWorkforce.Application.Interfaces;

public interface IApplicationService
{
    Task<(IEnumerable<JobApplication> Items, int Total, int TotalPages)> GetForJobSeekerAsync(int jobSeekerId, int page, int pageSize);
    Task<(IEnumerable<JobApplication> Items, int Total, int TotalPages)> GetForCompanyAsync(int companyId, int page, int pageSize, string? status);
    Task<JobApplication?> GetByIdAsync(int id);
    Task<bool> HasAppliedAsync(int jobSeekerId, int jobId);
    Task<JobApplication> CreateAsync(JobApplication application);
    Task<JobApplication> UpdateStatusAsync(int applicationId, ApplicationStatus newStatus, string? note, string changedByName);
    Task<bool> WithdrawAsync(int applicationId);
    Task<int> CountAllAsync();
    Task<int> CountForJobSeekerByStatusAsync(int jobSeekerId, ApplicationStatus status);
    Task<int> CountForCompanyAsync(int companyId);
    Task<int> CountForCompanyByStatusAsync(int companyId, ApplicationStatus status);
    Task<IEnumerable<JobApplication>> GetRecentForJobSeekerAsync(int jobSeekerId, int take);
    Task<IEnumerable<JobApplication>> GetRecentForCompanyAsync(int companyId, int take);
}
