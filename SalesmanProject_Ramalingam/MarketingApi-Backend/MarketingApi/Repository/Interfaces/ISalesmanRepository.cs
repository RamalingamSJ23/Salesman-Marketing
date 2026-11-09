using MarketingApi.DTOs.Common;
using MarketingApi.DTOs.Salesman;
using MarketingApi.Models;

namespace MarketingApi.Repository
{
    /// <summary>
    /// Repository interface for Salesman database operations.
    /// </summary>
    public interface ISalesmanRepository
    {
        Task<PagedResultDto<SalesmanListDto>> GetPagedSalesmenAsync(SalesmanQueryParameters parameters);
        Task<Salesman?> GetByIdAsync(long id);
        Task<Salesman?> GetByCodeAsync(string code);
        Task<bool> CodeExistsAsync(string code, long? excludeId = null);
        Task<bool> MobileExistsAsync(string mobile, long? excludeId = null);
        Task<bool> EmailExistsAsync(string email, long? excludeId = null);
        Task<List<SalesmanLookupDto>> GetActiveSalesmenLookupAsync();
        Task<Salesman> CreateAsync(Salesman salesman);
        Task UpdateAsync(Salesman salesman);
        Task DeleteAsync(Salesman salesman);
        Task<bool> IsAssignedToAnyTeamAsync(long salesmanId);
        Task<ActiveTeamAssignmentDto?> GetActiveTeamAssignmentAsync(long salesmanId);
        Task<SalesmanDeactivationRequest> CreateDeactivationRequestAsync(SalesmanDeactivationRequest request);
        Task<List<SalesmanDeactivationRequest>> GetPendingDeactivationRequestsAsync();
        Task<SalesmanDeactivationRequest?> GetDeactivationRequestByIdAsync(long id);
        Task UpdateDeactivationRequestAsync(SalesmanDeactivationRequest request);
        Task<SalesmanDeactivationRequest?> GetPendingRequestForSalesmanAsync(long salesmanId);
        Task DeactivateTeamAndMembersAsync(long teamId);
        Task DeactivateTeamMemberAsync(long teamId, long salesmanId);
        Task<List<Salesman>> GetSalesmenByIdsAsync(IEnumerable<long> ids);
    }
}
