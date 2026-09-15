using ElevateWorkforce.Application.Interfaces;
using ElevateWorkforce.Domain.Entities;
using ElevateWorkforce.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ElevateWorkforce.Application.Services;

public class CompanyService : ICompanyService
{
    private readonly ICompanyRepository _repository;
    private readonly IUserRepository _userRepository;

    public CompanyService(ICompanyRepository repository, IUserRepository userRepository)
    {
        _repository = repository;
        _userRepository = userRepository;
    }

    public Task<Company?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);
    public Task<Company> CreateAsync(Company company) => _repository.AddAsync(company);
    public Task UpdateAsync(Company company) => _repository.UpdateAsync(company);

    public async Task SetLogoAsync(Company company, string storedName, string originalName, string contentType, long size)
    {
        company.LogoStoredName = storedName;
        company.LogoFileName = originalName;
        company.LogoContentType = contentType;
        await _repository.UpdateAsync(company);
    }

    public async Task<(IEnumerable<Company> Items, int Total, int TotalPages)> AdminSearchAsync(
        string? keyword, string? status, int page, int pageSize)
    {
        var query = _repository.Query().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim().ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(k) ||
                c.Industry!.ToLower().Contains(k) ||
                c.Location!.ToLower().Contains(k));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<CompanyStatus>(status, true, out var st))
            query = query.Where(c => c.Status == st);

        query = query.OrderByDescending(c => c.CreatedAt);
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total, (int)Math.Ceiling(total / (double)pageSize));
    }

    public Task<int> CountTotalAsync() => _repository.CountAsync();
    public Task<int> CountPendingAsync() => _repository.CountPendingAsync();
    public Task<IEnumerable<Company>> GetRecentAsync(int take) => _repository.GetRecentAsync(take);

    public async Task<Company?> GetByEmployerUserIdAsync(int userId)
    {
        var employer = await _userRepository.QueryUsers()
            .Where(u => u.Id == userId)
            .Include(u => u.Employer)
            .Select(u => u.Employer)
            .FirstOrDefaultAsync();

        return employer is null ? null : await _repository.GetByIdAsync(employer.CompanyId);
    }

    public async Task<int> CountJobsAsync(int companyId) =>
        await _repository.Query()
            .Where(c => c.Id == companyId)
            .SelectMany(c => c.Jobs)
            .CountAsync(j => !j.IsDeleted);

    public async Task UpdateStatusAsync(int companyId, CompanyStatus status)
    {
        var company = await _repository.GetByIdAsync(companyId);
        if (company is null) return;
        company.Status = status;
        await _repository.UpdateAsync(company);
    }
}