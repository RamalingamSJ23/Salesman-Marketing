using MarketingApi.DTOs.Common;
using MarketingApi.DTOs.Salesman;
using MarketingApi.Models.Entities;

namespace MarketingApi.Repository.Interfaces;

/// <summary>
/// Repository interface for Salesman database operations.
/// </summary>
public interface ISalesmanRepository
{
    Task<PagedResultDto<SalesmanListDto>> GetPagedSalesmenAsync(SalesmanQueryParameters parameters);
    Task<Salesman?> GetByIdAsync(long id, bool trackChanges = false);
    Task<bool> CodeExistsAsync(string code, long? excludeId = null);
    Task<bool> MobileExistsAsync(string mobile, long? excludeId = null);
    Task<bool> EmailExistsAsync(string email, long? excludeId = null);
    Task<List<SalesmanLookupDto>> GetActiveSalesmenLookupAsync();
    Task<Salesman> CreateAsync(Salesman salesman);
    Task UpdateAsync(Salesman salesman);
    Task<ActiveTeamAssignmentDto?> GetActiveTeamAssignmentAsync(long salesmanId);
    Task<SalesmanDeactivationRequest> CreateDeactivationRequestAsync(SalesmanDeactivationRequest request);
    Task<List<SalesmanDeactivationRequest>> GetPendingDeactivationRequestsAsync();
    Task<SalesmanDeactivationRequest?> GetDeactivationRequestByIdAsync(long id);
    Task UpdateDeactivationRequestAsync(SalesmanDeactivationRequest request);
    Task<SalesmanDeactivationRequest?> GetPendingRequestForSalesmanAsync(long salesmanId);
    Task<List<Salesman>> GetSalesmenByIdsAsync(IEnumerable<long> ids);
    Task<SalesmanDeactivationRequest> ApproveDeactivationAtomicallyAsync(
        SalesmanDeactivationRequest request,
        Salesman salesman,
        ActiveTeamAssignmentDto? currentAssignment,
        string processedBy);
    Task ActivateSalesmanAndCancelPendingRequestsAsync(Salesman salesman, string processedBy);
}
