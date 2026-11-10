using MarketingApi.DTOs.Common;
using MarketingApi.DTOs.MarketingTeam;
using MarketingApi.Models.Entities;

namespace MarketingApi.Repository.Interfaces;

/// <summary>
/// Repository interface for Marketing Team database operations.
/// </summary>
public interface IMarketingTeamRepository
{
    Task<PagedResultDto<MarketingTeamListDto>> GetPagedTeamsAsync(MarketingTeamQueryParameters parameters);
    Task<MarketingTeam?> GetByIdWithDetailsAsync(long id);
    Task<bool> CodeExistsAsync(string code, long? excludeId = null);
    Task<MarketingTeam> CreateTeamWithMembersAsync(MarketingTeam team, List<MarketingTeamDetail> details);
    Task AddTeamMemberAsync(MarketingTeamDetail detail);
    Task UpdateTeamWithMembersAsync(long teamId, UpdateMarketingTeamDto dto);
    Task DeactivateTeamWithMembersAsync(long teamId);
    Task ActivateTeamAsync(long teamId);
    Task<string?> GetActiveTeamCodeForSalesmanAsync(long salesmanId, long? excludeTeamId = null);
    Task<Dictionary<long, string>> GetActiveTeamCodesForSalesmenAsync(IEnumerable<long> salesmanIds, long? excludeTeamId = null);
}
