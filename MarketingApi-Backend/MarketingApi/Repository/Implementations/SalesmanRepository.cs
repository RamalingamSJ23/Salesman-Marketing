using MarketingApi.Common;
using MarketingApi.Data;
using MarketingApi.DTOs.Common;
using MarketingApi.DTOs.Salesman;
using MarketingApi.Enums;
using MarketingApi.Models.Entities;
using MarketingApi.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MarketingApi.Repository.Implementations;

/// <summary>
/// Repository implementation for Salesman database operations using EF Core.
/// </summary>
public class SalesmanRepository : ISalesmanRepository
{
    private readonly SalesMarketingDbContext _dbContext;

    public SalesmanRepository(SalesMarketingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Retrieves a paginated, sorted list of salesmen with optional search and status filter.
    /// </summary>
    public async Task<PagedResultDto<SalesmanListDto>> GetPagedSalesmenAsync(SalesmanQueryParameters parameters)
    {
        var query = _dbContext.Salesmen.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var escaped = SearchHelper.EscapeLikePattern(parameters.Search.Trim());
            var pattern = $"%{escaped}%";
            query = query.Where(s =>
                EF.Functions.ILike(s.SalesmanCode, pattern, @"\") ||
                EF.Functions.ILike(s.SalesmanName, pattern, @"\") ||
                (s.MobileNo != null && EF.Functions.ILike(s.MobileNo, pattern, @"\")));
        }

        if (parameters.Status.HasValue)
        {
            query = query.Where(s => s.Status == parameters.Status.Value);
        }

        var totalCount = await query.CountAsync();

        var isDesc = string.Equals(parameters.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
        var sortBy = (parameters.SortBy ?? "salesmancode").Trim().ToLowerInvariant();

        query = (sortBy, isDesc) switch
        {
            ("salesmanname", false) => query.OrderBy(s => s.SalesmanName).ThenBy(s => s.SalesmanCode),
            ("salesmanname", true) => query.OrderByDescending(s => s.SalesmanName).ThenBy(s => s.SalesmanCode),
            ("mobileno", false) => query.OrderBy(s => s.MobileNo).ThenBy(s => s.SalesmanCode),
            ("mobileno", true) => query.OrderByDescending(s => s.MobileNo).ThenBy(s => s.SalesmanCode),
            ("email", false) => query.OrderBy(s => s.Email).ThenBy(s => s.SalesmanCode),
            ("email", true) => query.OrderByDescending(s => s.Email).ThenBy(s => s.SalesmanCode),
            ("joiningdate", false) => query.OrderBy(s => s.JoiningDate).ThenBy(s => s.SalesmanCode),
            ("joiningdate", true) => query.OrderByDescending(s => s.JoiningDate).ThenBy(s => s.SalesmanCode),
            ("status", false) => query.OrderBy(s => s.Status).ThenBy(s => s.SalesmanCode),
            ("status", true) => query.OrderByDescending(s => s.Status).ThenBy(s => s.SalesmanCode),
            ("salesmancode", true) => query.OrderByDescending(s => s.SalesmanCode),
            _ => query.OrderBy(s => s.SalesmanCode)
        };

        var items = await query
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(s => new SalesmanListDto
            {
                Id = s.Id,
                Salesmancode = s.SalesmanCode,
                Salesmanname = s.SalesmanName,
                Mobileno = s.MobileNo,
                Email = s.Email,
                Joiningdate = s.JoiningDate,
                Status = s.Status,
                Createddate = s.CreatedDate,
                Modifieddate = s.ModifiedDate
            })
            .ToListAsync();

        var salesmanIds = items.Select(i => i.Id).ToList();
        var pendingStatus = nameof(DeactivationRequestStatus.Pending);
        var pendingRequests = await _dbContext.SalesmanDeactivationRequests
            .AsNoTracking()
            .Where(r => r.Status == pendingStatus && salesmanIds.Contains(r.SalesmanId))
            .ToDictionaryAsync(r => r.SalesmanId);

        foreach (var item in items)
        {
            if (pendingRequests.TryGetValue(item.Id, out var req))
            {
                item.HasPendingDeactivation = true;
                item.PendingDeactivationTeam = req.TeamCode ?? req.TeamName;
                item.PendingDeactivationRole = req.RoleInTeam;
            }
        }

        return new PagedResultDto<SalesmanListDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize
        };
    }

    public async Task<Salesman?> GetByIdAsync(long id)
    {
        return await _dbContext.Salesmen
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<bool> CodeExistsAsync(string code, long? excludeId = null)
    {
        var cleanCode = code.Trim().ToUpperInvariant();
        return await _dbContext.Salesmen
            .AnyAsync(s => s.SalesmanCode == cleanCode && (!excludeId.HasValue || s.Id != excludeId.Value));
    }

    public async Task<bool> MobileExistsAsync(string mobile, long? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(mobile)) return false;
        var clean = mobile.Trim();
        return await _dbContext.Salesmen
            .AnyAsync(s => s.MobileNo == clean && (!excludeId.HasValue || s.Id != excludeId.Value));
    }

    public async Task<bool> EmailExistsAsync(string email, long? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        var clean = email.Trim().ToLowerInvariant();
        return await _dbContext.Salesmen
            .AnyAsync(s => s.Email == clean && (!excludeId.HasValue || s.Id != excludeId.Value));
    }

    /// <summary>
    /// Returns active salesmen for dropdowns in a single query, enriched with current active team assignment.
    /// </summary>
    public async Task<List<SalesmanLookupDto>> GetActiveSalesmenLookupAsync()
    {
        const short activeStatus = (short)RecordStatus.Active;
        const short leaderRole = (short)SalesmanRole.TeamLeader;

        return await _dbContext.Salesmen
            .AsNoTracking()
            .Where(s => s.Status == activeStatus)
            .OrderBy(s => s.SalesmanName)
            .Select(s => new SalesmanLookupDto
            {
                Id = s.Id,
                Salesmancode = s.SalesmanCode,
                Salesmanname = s.SalesmanName,
                Status = s.Status,
                ActiveTeamId = _dbContext.MarketingTeams
                    .Where(t => t.TeamLeaderId == s.Id && t.Status == activeStatus)
                    .Select(t => (long?)t.Id)
                    .FirstOrDefault()
                    ?? _dbContext.MarketingTeamDetails
                        .Where(d => d.SalesmanId == s.Id && d.Status == activeStatus && d.MarketingTeam.Status == activeStatus)
                        .Select(d => (long?)d.MarketingTeamId)
                        .FirstOrDefault(),
                ActiveTeamCode = _dbContext.MarketingTeams
                    .Where(t => t.TeamLeaderId == s.Id && t.Status == activeStatus)
                    .Select(t => t.TeamCode)
                    .FirstOrDefault()
                    ?? _dbContext.MarketingTeamDetails
                        .Where(d => d.SalesmanId == s.Id && d.Status == activeStatus && d.MarketingTeam.Status == activeStatus)
                        .Select(d => d.MarketingTeam.TeamCode)
                        .FirstOrDefault(),
                IsLeaderInActiveTeam = _dbContext.MarketingTeams
                    .Any(t => t.TeamLeaderId == s.Id && t.Status == activeStatus)
                    || _dbContext.MarketingTeamDetails
                        .Any(d => d.SalesmanId == s.Id && d.Status == activeStatus && d.MarketingTeam.Status == activeStatus && d.Role == leaderRole)
            })
            .ToListAsync();
    }

    public async Task<Salesman> CreateAsync(Salesman salesman)
    {
        _dbContext.Salesmen.Add(salesman);
        await _dbContext.SaveChangesAsync();
        return salesman;
    }

    public async Task UpdateAsync(Salesman salesman)
    {
        _dbContext.Salesmen.Update(salesman);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<List<Salesman>> GetSalesmenByIdsAsync(IEnumerable<long> ids)
    {
        return await _dbContext.Salesmen
            .AsNoTracking()
            .Where(s => ids.Contains(s.Id))
            .ToListAsync();
    }

    public async Task<ActiveTeamAssignmentDto?> GetActiveTeamAssignmentAsync(long salesmanId)
    {
        const short activeStatus = (short)RecordStatus.Active;
        const short leaderRole = (short)SalesmanRole.TeamLeader;

        var leaderTeam = await _dbContext.MarketingTeams
            .AsNoTracking()
            .Where(t => t.TeamLeaderId == salesmanId && t.Status == activeStatus)
            .Select(t => new ActiveTeamAssignmentDto
            {
                TeamId = t.Id,
                TeamCode = t.TeamCode,
                TeamName = t.TeamName,
                RoleType = TeamRoleType.Leader
            })
            .FirstOrDefaultAsync();

        if (leaderTeam != null)
        {
            return leaderTeam;
        }

        return await _dbContext.MarketingTeamDetails
            .AsNoTracking()
            .Where(d => d.SalesmanId == salesmanId && d.Status == activeStatus && d.MarketingTeam.Status == activeStatus)
            .Select(d => new ActiveTeamAssignmentDto
            {
                TeamId = d.MarketingTeamId,
                TeamCode = d.MarketingTeam.TeamCode,
                TeamName = d.MarketingTeam.TeamName,
                RoleType = d.Role == leaderRole ? TeamRoleType.Leader : TeamRoleType.Member
            })
            .FirstOrDefaultAsync();
    }

    public async Task<SalesmanDeactivationRequest> CreateDeactivationRequestAsync(SalesmanDeactivationRequest request)
    {
        _dbContext.SalesmanDeactivationRequests.Add(request);
        await _dbContext.SaveChangesAsync();
        return request;
    }

    public async Task<List<SalesmanDeactivationRequest>> GetPendingDeactivationRequestsAsync()
    {
        var pendingStatus = nameof(DeactivationRequestStatus.Pending);
        return await _dbContext.SalesmanDeactivationRequests
            .AsNoTracking()
            .Where(r => r.Status == pendingStatus)
            .OrderByDescending(r => r.RequestedDate)
            .ToListAsync();
    }

    public async Task<SalesmanDeactivationRequest?> GetDeactivationRequestByIdAsync(long id)
    {
        return await _dbContext.SalesmanDeactivationRequests
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task UpdateDeactivationRequestAsync(SalesmanDeactivationRequest request)
    {
        _dbContext.SalesmanDeactivationRequests.Update(request);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<SalesmanDeactivationRequest?> GetPendingRequestForSalesmanAsync(long salesmanId)
    {
        var pendingStatus = nameof(DeactivationRequestStatus.Pending);
        return await _dbContext.SalesmanDeactivationRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.SalesmanId == salesmanId && r.Status == pendingStatus);
    }

    /// <summary>
    /// Atomically deactivates the salesman, updates their current team/member status, and approves the request in one transaction.
    /// </summary>
    public async Task<SalesmanDeactivationRequest> ApproveDeactivationAtomicallyAsync(
        SalesmanDeactivationRequest request,
        Salesman salesman,
        ActiveTeamAssignmentDto? currentAssignment,
        string processedBy)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            const short inactiveStatus = (short)RecordStatus.Inactive;

            if (currentAssignment != null)
            {
                request.TeamId = currentAssignment.TeamId;
                request.TeamCode = currentAssignment.TeamCode;
                request.TeamName = currentAssignment.TeamName;
                request.RoleInTeam = currentAssignment.RoleInTeam;

                if (currentAssignment.RoleType == TeamRoleType.Leader)
                {
                    var team = await _dbContext.MarketingTeams
                        .FirstOrDefaultAsync(t => t.Id == currentAssignment.TeamId);
                    if (team != null)
                    {
                        team.Status = inactiveStatus;
                    }

                    var details = await _dbContext.MarketingTeamDetails
                        .Where(d => d.MarketingTeamId == currentAssignment.TeamId)
                        .ToListAsync();

                    foreach (var detail in details)
                    {
                        detail.Status = inactiveStatus;
                    }
                }
                else
                {
                    var details = await _dbContext.MarketingTeamDetails
                        .Where(d => d.MarketingTeamId == currentAssignment.TeamId && d.SalesmanId == salesman.Id)
                        .ToListAsync();

                    foreach (var detail in details)
                    {
                        detail.Status = inactiveStatus;
                    }

                    var team = await _dbContext.MarketingTeams
                        .FirstOrDefaultAsync(t => t.Id == currentAssignment.TeamId);
                    if (team != null)
                    {
                        _dbContext.Entry(team).State = EntityState.Modified;
                    }
                }
            }

            // Attach untracked salesman from GetByIdAsync and deactivate
            _dbContext.Salesmen.Attach(salesman);
            salesman.Status = inactiveStatus;

            request.Status = nameof(DeactivationRequestStatus.Approved);
            request.ProcessedDate = DateTime.UtcNow;
            request.ProcessedBy = processedBy;

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            return request;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task ActivateSalesmanAndCancelPendingRequestsAsync(Salesman salesman, string processedBy)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            _dbContext.Salesmen.Attach(salesman);
            salesman.Status = (short)RecordStatus.Active;

            var pendingStatus = nameof(DeactivationRequestStatus.Pending);
            var openRequests = await _dbContext.SalesmanDeactivationRequests
                .Where(r => r.SalesmanId == salesman.Id && r.Status == pendingStatus)
                .ToListAsync();

            var now = DateTime.UtcNow;
            foreach (var openRequest in openRequests)
            {
                openRequest.Status = nameof(DeactivationRequestStatus.Cancelled);
                openRequest.ProcessedDate = now;
                openRequest.ProcessedBy = processedBy;
                openRequest.Notes = "Cancelled automatically when salesman was activated.";
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
}
