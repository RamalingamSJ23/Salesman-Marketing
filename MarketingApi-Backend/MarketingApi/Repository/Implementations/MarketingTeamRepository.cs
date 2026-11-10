using MarketingApi.Common;
using MarketingApi.Data;
using MarketingApi.DTOs.Common;
using MarketingApi.DTOs.MarketingTeam;
using MarketingApi.Enums;
using MarketingApi.Models.Entities;
using MarketingApi.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MarketingApi.Repository.Implementations;

/// <summary>
/// Repository implementation for Marketing Team database operations using EF Core.
/// Manages master-detail transactions and row merge.
/// </summary>
public class MarketingTeamRepository : IMarketingTeamRepository
{
    private readonly SalesMarketingDbContext _dbContext;

    public MarketingTeamRepository(SalesMarketingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Retrieves a paginated, sorted list of teams with team leader name and salesman count.
    /// </summary>
    public async Task<PagedResultDto<MarketingTeamListDto>> GetPagedTeamsAsync(MarketingTeamQueryParameters parameters)
    {
        const short activeStatus = (short)RecordStatus.Active;
        var query = _dbContext.MarketingTeams.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var escaped = SearchHelper.EscapeLikePattern(parameters.Search.Trim());
            var pattern = $"%{escaped}%";
            query = query.Where(t =>
                EF.Functions.ILike(t.TeamCode, pattern, @"\") ||
                EF.Functions.ILike(t.TeamName, pattern, @"\"));
        }

        if (parameters.Status.HasValue)
        {
            query = query.Where(t => t.Status == parameters.Status.Value);
        }

        var totalCount = await query.CountAsync();

        var isDesc = string.Equals(parameters.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
        var sortBy = (parameters.SortBy ?? "teamcode").Trim().ToLowerInvariant();

        query = (sortBy, isDesc) switch
        {
            ("teamname", false) => query.OrderBy(t => t.TeamName).ThenBy(t => t.TeamCode),
            ("teamname", true) => query.OrderByDescending(t => t.TeamName).ThenBy(t => t.TeamCode),
            ("teamleadername", false) => query.OrderBy(t => t.TeamLeader.SalesmanName).ThenBy(t => t.TeamCode),
            ("teamleadername", true) => query.OrderByDescending(t => t.TeamLeader.SalesmanName).ThenBy(t => t.TeamCode),
            ("salesmancount", false) => query.OrderBy(t => t.Status == activeStatus
                ? t.MarketingTeamDetails.Count(d => d.Status == activeStatus)
                : t.MarketingTeamDetails.Count()).ThenBy(t => t.TeamCode),
            ("salesmancount", true) => query.OrderByDescending(t => t.Status == activeStatus
                ? t.MarketingTeamDetails.Count(d => d.Status == activeStatus)
                : t.MarketingTeamDetails.Count()).ThenBy(t => t.TeamCode),
            ("status", false) => query.OrderBy(t => t.Status).ThenBy(t => t.TeamCode),
            ("status", true) => query.OrderByDescending(t => t.Status).ThenBy(t => t.TeamCode),
            ("teamcode", true) => query.OrderByDescending(t => t.TeamCode),
            _ => query.OrderBy(t => t.TeamCode)
        };

        var items = await query
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(t => new MarketingTeamListDto
            {
                Id = t.Id,
                Teamcode = t.TeamCode,
                Teamname = t.TeamName,
                Teamleadername = t.TeamLeader.SalesmanName,
                Salesmancount = t.Status == activeStatus
                    ? t.MarketingTeamDetails.Count(d => d.Status == activeStatus)
                    : t.MarketingTeamDetails.Count(),
                Status = t.Status
            })
            .ToListAsync();

        return new PagedResultDto<MarketingTeamListDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize
        };
    }

    public async Task<MarketingTeam?> GetByIdWithDetailsAsync(long id)
    {
        return await _dbContext.MarketingTeams
            .AsNoTracking()
            .Include(t => t.TeamLeader)
            .Include(t => t.MarketingTeamDetails)
                .ThenInclude(d => d.Salesman)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<bool> CodeExistsAsync(string code, long? excludeId = null)
    {
        var cleanCode = code.Trim().ToUpperInvariant();
        return await _dbContext.MarketingTeams
            .AnyAsync(t => t.TeamCode == cleanCode && (!excludeId.HasValue || t.Id != excludeId.Value));
    }

    public async Task<MarketingTeam> CreateTeamWithMembersAsync(MarketingTeam team, List<MarketingTeamDetail> details)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            _dbContext.MarketingTeams.Add(team);
            await _dbContext.SaveChangesAsync();

            foreach (var detail in details)
            {
                detail.MarketingTeamId = team.Id;
                _dbContext.MarketingTeamDetails.Add(detail);
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            return team;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task AddTeamMemberAsync(MarketingTeamDetail detail)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var existingInactive = await _dbContext.MarketingTeamDetails
                .FirstOrDefaultAsync(d => d.MarketingTeamId == detail.MarketingTeamId && d.SalesmanId == detail.SalesmanId);

            if (existingInactive != null)
            {
                existingInactive.Role = detail.Role;
                existingInactive.TargetAmount = detail.TargetAmount;
                existingInactive.Status = detail.Status;
            }
            else
            {
                _dbContext.MarketingTeamDetails.Add(detail);
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Updates a marketing team header and merges member rows in a single transaction (Rule R10).
    /// Removed rows are marked Inactive to preserve membership history (H15).
    /// </summary>
    public async Task UpdateTeamWithMembersAsync(long teamId, UpdateMarketingTeamDto dto)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var team = await _dbContext.MarketingTeams
                .Include(t => t.MarketingTeamDetails)
                .FirstOrDefaultAsync(t => t.Id == teamId);

            if (team == null)
            {
                throw new KeyNotFoundException($"Team with ID {teamId} not found.");
            }

            team.TeamName = dto.Teamname.Trim();
            team.TeamLeaderId = dto.Teamleaderid;
            team.Description = dto.Description?.Trim();

            var existingDetails = team.MarketingTeamDetails.ToList();
            var incomingMembers = dto.Members;

            var incomingExistingIds = incomingMembers
                .Where(m => m.Id.HasValue && m.Id.Value > 0)
                .Select(m => m.Id!.Value)
                .ToHashSet();

            // Mark removed rows Inactive instead of hard deleting (H15)
            foreach (var detail in existingDetails.Where(d => !incomingExistingIds.Contains(d.Id)))
            {
                detail.Status = (short)RecordStatus.Inactive;
            }

            foreach (var memberDto in incomingMembers)
            {
                if (memberDto.Id.HasValue && memberDto.Id.Value > 0)
                {
                    var existingRow = existingDetails.FirstOrDefault(d => d.Id == memberDto.Id.Value);
                    if (existingRow != null)
                    {
                        // Do not allow changing SalesmanId on an existing row (H13)
                        existingRow.Role = memberDto.Role;
                        existingRow.TargetAmount = memberDto.Targetamount;
                        existingRow.Status = memberDto.Status;
                    }
                }
                else
                {
                    // Reactivate an existing inactive detail row for the same salesman if present
                    var previousInactive = existingDetails.FirstOrDefault(d => d.SalesmanId == memberDto.Salesmanid);
                    if (previousInactive != null)
                    {
                        previousInactive.Role = memberDto.Role;
                        previousInactive.TargetAmount = memberDto.Targetamount;
                        previousInactive.Status = memberDto.Status;
                    }
                    else
                    {
                        _dbContext.MarketingTeamDetails.Add(new MarketingTeamDetail
                        {
                            MarketingTeamId = team.Id,
                            SalesmanId = memberDto.Salesmanid,
                            Role = memberDto.Role,
                            TargetAmount = memberDto.Targetamount,
                            Status = memberDto.Status
                        });
                    }
                }
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task DeactivateTeamWithMembersAsync(long teamId)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var team = await _dbContext.MarketingTeams
                .Include(t => t.MarketingTeamDetails)
                .FirstOrDefaultAsync(t => t.Id == teamId);

            if (team == null)
            {
                throw new KeyNotFoundException($"Team with ID {teamId} not found.");
            }

            team.Status = (short)RecordStatus.Inactive;

            foreach (var detail in team.MarketingTeamDetails)
            {
                detail.Status = (short)RecordStatus.Inactive;
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task ActivateTeamAsync(long teamId)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var team = await _dbContext.MarketingTeams
                .FirstOrDefaultAsync(t => t.Id == teamId);

            if (team == null)
            {
                throw new KeyNotFoundException($"Team with ID {teamId} not found.");
            }

            team.Status = (short)RecordStatus.Active;

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<string?> GetActiveTeamCodeForSalesmanAsync(long salesmanId, long? excludeTeamId = null)
    {
        var map = await GetActiveTeamCodesForSalesmenAsync(new[] { salesmanId }, excludeTeamId);
        return map.TryGetValue(salesmanId, out var code) ? code : null;
    }

    /// <summary>
    /// Batch-loads active team codes for all requested salesman IDs in one query (M6).
    /// </summary>
    public async Task<Dictionary<long, string>> GetActiveTeamCodesForSalesmenAsync(
        IEnumerable<long> salesmanIds,
        long? excludeTeamId = null)
    {
        var ids = salesmanIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<long, string>();
        }

        const short activeStatus = (short)RecordStatus.Active;

        var activeDetails = await _dbContext.MarketingTeamDetails
            .AsNoTracking()
            .Where(d => ids.Contains(d.SalesmanId)
                     && d.Status == activeStatus
                     && d.MarketingTeam.Status == activeStatus
                     && (!excludeTeamId.HasValue || d.MarketingTeamId != excludeTeamId.Value))
            .Select(d => new { d.SalesmanId, d.MarketingTeam.TeamCode })
            .ToListAsync();

        var activeLeaders = await _dbContext.MarketingTeams
            .AsNoTracking()
            .Where(t => ids.Contains(t.TeamLeaderId)
                     && t.Status == activeStatus
                     && (!excludeTeamId.HasValue || t.Id != excludeTeamId.Value))
            .Select(t => new { SalesmanId = t.TeamLeaderId, t.TeamCode })
            .ToListAsync();

        var result = new Dictionary<long, string>();
        foreach (var entry in activeDetails.Concat(activeLeaders))
        {
            result.TryAdd(entry.SalesmanId, entry.TeamCode);
        }

        return result;
    }
}
