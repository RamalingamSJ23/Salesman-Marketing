using MarketingApi.Common;
using MarketingApi.DTOs.Common;
using MarketingApi.DTOs.MarketingTeam;

namespace MarketingApi.Service.Interfaces;

/// <summary>
/// Service interface for Marketing Team business logic and rules.
/// </summary>
public interface IMarketingTeamService
{
    Task<ServiceResult<PagedResultDto<MarketingTeamListDto>>> GetPagedTeamsAsync(MarketingTeamQueryParameters parameters);
    Task<ServiceResult<MarketingTeamDto>> GetByIdAsync(long id);
    Task<ServiceResult<MarketingTeamDto>> CreateTeamAsync(CreateMarketingTeamDto dto);
    Task<ServiceResult<MarketingTeamDto>> AddSalesmanToTeamAsync(long id, MarketingTeamDetailInputDto dto);
    Task<ServiceResult<MarketingTeamDto>> UpdateTeamAsync(long id, UpdateMarketingTeamDto dto);
    Task<ServiceResult<MarketingTeamDto>> DeactivateTeamAsync(long id);
    Task<ServiceResult<MarketingTeamDto>> ActivateTeamAsync(long id);
    List<EnumOptionDto> GetRoleOptions();
}
