using MarketingApi.Common;
using MarketingApi.DTOs.Common;
using MarketingApi.DTOs.Salesman;

namespace MarketingApi.Service
{
    /// <summary>
    /// Service interface for Salesman business logic and rules.
    /// </summary>
    public interface ISalesmanService
    {
        Task<ServiceResult<PagedResultDto<SalesmanListDto>>> GetPagedSalesmenAsync(SalesmanQueryParameters parameters);
        Task<ServiceResult<SalesmanDto>> GetByIdAsync(long id);
        Task<ServiceResult<SalesmanDto>> CreateSalesmanAsync(CreateSalesmanDto dto);
        Task<ServiceResult<SalesmanDto>> UpdateSalesmanAsync(long id, UpdateSalesmanDto dto);
        Task<ServiceResult<DeactivateSalesmanResultDto>> DeactivateSalesmanAsync(long id, string requestedBy = "superviser");
        Task<ServiceResult<SalesmanDto>> ActivateSalesmanAsync(long id);
        Task<ServiceResult> DeleteSalesmanAsync(long id);
        Task<ServiceResult<List<SalesmanLookupDto>>> GetActiveSalesmenLookupAsync();
        Task<ServiceResult<ActiveTeamAssignmentDto?>> CheckSalesmanTeamAssignmentAsync(long salesmanId);
        Task<ServiceResult<List<SalesmanDeactivationRequestDto>>> GetPendingDeactivationRequestsAsync();
        Task<ServiceResult<SalesmanDeactivationRequestDto>> ApproveDeactivationRequestAsync(long requestId, string processedBy = "marketing");
        Task<ServiceResult<SalesmanDeactivationRequestDto>> RejectDeactivationRequestAsync(long requestId, string processedBy = "marketing", string? notes = null);
    }
}
