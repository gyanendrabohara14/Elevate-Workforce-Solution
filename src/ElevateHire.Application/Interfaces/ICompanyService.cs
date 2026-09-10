using ElevateHire.Domain.Entities;
using ElevateHire.Domain.Enums;

namespace ElevateHire.Application.Interfaces;

public interface ICompanyService
{
    Task<Company?> GetByIdAsync(int id);
    Task<Company> CreateAsync(Company company);
    Task UpdateAsync(Company company);
    Task SetLogoAsync(Company company, string storedName, string originalName, string contentType, long size);
    Task<(IEnumerable<Company> Items, int Total, int TotalPages)> AdminSearchAsync(string? keyword, string? status, int page, int pageSize);
    Task<int> CountTotalAsync();
    Task<int> CountPendingAsync();
    Task<IEnumerable<Company>> GetRecentAsync(int take);
    Task<Company?> GetByEmployerUserIdAsync(int userId);
    Task<int> CountJobsAsync(int companyId);
    Task UpdateStatusAsync(int companyId, CompanyStatus status);
}
