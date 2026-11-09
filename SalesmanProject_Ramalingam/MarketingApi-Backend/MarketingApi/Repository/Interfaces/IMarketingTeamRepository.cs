using MarketingApi.DTOs.Common;
using MarketingApi.DTOs.MarketingTeam;
using MarketingApi.Models;

namespace MarketingApi.Repository
{
    /// <summary>
    /// Repository interface for Marketing Team database operations.
    /// </summary>
    public interface IMarketingTeamRepository
    {
        Task<PagedResultDto<MarketingTeamListDto>> GetPagedTeamsAsync(MarketingTeamQueryParameters parameters);
        Task<Marketingteam?> GetByIdWithDetailsAsync(long id);
        Task<bool> CodeExistsAsync(string code, long? excludeId = null);
        Task<Marketingteam> CreateTeamWithMembersAsync(Marketingteam team, List<Marketingteamdetail> details);
        Task AddTeamMemberAsync(Marketingteamdetail detail);
        Task UpdateTeamWithMembersAsync(long teamId, UpdateMarketingTeamDto dto);
        Task DeactivateTeamWithMembersAsync(long teamId);
        Task ActivateTeamAsync(long teamId);
        Task DeleteTeamAsync(long teamId);
        Task<string?> GetActiveTeamCodeForSalesmanAsync(long salesmanId, long? excludeTeamId = null);
    }
}
