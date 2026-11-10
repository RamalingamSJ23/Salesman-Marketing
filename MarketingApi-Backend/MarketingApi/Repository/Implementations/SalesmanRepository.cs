using MarketingApi.Data;
using MarketingApi.DTOs.Common;
using MarketingApi.DTOs.Salesman;
using MarketingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace MarketingApi.Repository
{
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
        /// Retrieves a paginated list of salesmen with optional search and status filter.
        /// </summary>
        public async Task<PagedResultDto<SalesmanListDto>> GetPagedSalesmenAsync(SalesmanQueryParameters parameters)
        {
            var query = _dbContext.Salesmen.AsNoTracking().AsQueryable();

            // Search filter by code, name, or mobile
            if (!string.IsNullOrWhiteSpace(parameters.Search))
            {
                var term = parameters.Search.Trim().ToLower();
                query = query.Where(s =>
                    EF.Functions.ILike(s.Salesmancode, $"%{term}%") ||
                    EF.Functions.ILike(s.Salesmanname, $"%{term}%") ||
                    (s.Mobileno != null && EF.Functions.ILike(s.Mobileno, $"%{term}%")));
            }

            // Status filter (1 = Active, 2 = Inactive, null = All)
            if (parameters.Status.HasValue)
            {
                query = query.Where(s => s.Status == parameters.Status.Value);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(s => s.Salesmancode)
                .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .Select(s => new SalesmanListDto
                {
                    Id = s.Id,
                    Salesmancode = s.Salesmancode,
                    Salesmanname = s.Salesmanname,
                    Mobileno = s.Mobileno,
                    Email = s.Email,
                    Joiningdate = s.Joiningdate,
                    Status = s.Status,
                    Createddate = s.Createddate,
                    Modifieddate = s.Modifieddate
                })
                .ToListAsync();

            var salesmanIds = items.Select(i => i.Id).ToList();
            var pendingRequests = await _dbContext.SalesmanDeactivationRequests
                .AsNoTracking()
                .Where(r => r.Status == "Pending" && salesmanIds.Contains(r.Salesmanid))
                .ToDictionaryAsync(r => r.Salesmanid);

            foreach (var item in items)
            {
                if (pendingRequests.TryGetValue(item.Id, out var req))
                {
                    item.HasPendingDeactivation = true;
                    item.PendingDeactivationTeam = req.Teamcode ?? req.Teamname;
                    item.PendingDeactivationRole = req.Roleinteam;
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

        /// <summary>
        /// Gets a salesman by ID.
        /// </summary>
        public async Task<Salesman?> GetByIdAsync(long id)
        {
            return await _dbContext.Salesmen
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        /// <summary>
        /// Gets a salesman by unique code.
        /// </summary>
        public async Task<Salesman?> GetByCodeAsync(string code)
        {
            var cleanCode = code.Trim().ToUpper();
            return await _dbContext.Salesmen
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Salesmancode.ToUpper() == cleanCode);
        }

        /// <summary>
        /// Checks whether a salesman code already exists (optionally excluding a given ID when updating).
        /// </summary>
        public async Task<bool> CodeExistsAsync(string code, long? excludeId = null)
        {
            var cleanCode = code.Trim().ToUpper();
            return await _dbContext.Salesmen
                .AnyAsync(s => s.Salesmancode.ToUpper() == cleanCode && (!excludeId.HasValue || s.Id != excludeId.Value));
        }

        /// <summary>
        /// Checks whether a mobile number already exists (optionally excluding a given ID when updating).
        /// </summary>
        public async Task<bool> MobileExistsAsync(string mobile, long? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(mobile)) return false;
            var clean = mobile.Trim();
            return await _dbContext.Salesmen
                .AnyAsync(s => s.Mobileno != null && s.Mobileno.Trim() == clean && (!excludeId.HasValue || s.Id != excludeId.Value));
        }

        /// <summary>
        /// Checks whether an email address already exists (case-insensitive, optionally excluding a given ID).
        /// </summary>
        public async Task<bool> EmailExistsAsync(string email, long? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            var clean = email.Trim().ToLower();
            return await _dbContext.Salesmen
                .AnyAsync(s => s.Email != null && s.Email.Trim().ToLower() == clean && (!excludeId.HasValue || s.Id != excludeId.Value));
        }

        /// <summary>
        /// Returns active salesmen (Status = 1) for dropdowns, enriched with their current active team assignment if any.
        /// </summary>
        public async Task<List<SalesmanLookupDto>> GetActiveSalesmenLookupAsync()
        {
            var activeAssignments = await _dbContext.Marketingteamdetails
                .AsNoTracking()
                .Include(d => d.Marketingteam)
                .Where(d => d.Status == 1 && d.Marketingteam.Status == 1)
                .Select(d => new
                {
                    d.Salesmanid,
                    d.Marketingteamid,
                    Teamcode = d.Marketingteam.Teamcode,
                    IsLeader = d.Role == 1
                })
                .ToListAsync();

            var activeTeamLeaders = await _dbContext.Marketingteams
                .AsNoTracking()
                .Where(t => t.Status == 1)
                .Select(t => new
                {
                    Salesmanid = t.Teamleaderid,
                    Marketingteamid = t.Id,
                    Teamcode = t.Teamcode,
                    IsLeader = true
                })
                .ToListAsync();

            var assignmentMap = activeAssignments
                .Concat(activeTeamLeaders)
                .GroupBy(a => a.Salesmanid)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.IsLeader).First());

            var salesmen = await _dbContext.Salesmen
                .AsNoTracking()
                .Where(s => s.Status == 1) // 1 = Active
                .OrderBy(s => s.Salesmanname)
                .ToListAsync();

            return salesmen.Select(s =>
            {
                assignmentMap.TryGetValue(s.Id, out var assignment);
                return new SalesmanLookupDto
                {
                    Id = s.Id,
                    Salesmancode = s.Salesmancode,
                    Salesmanname = s.Salesmanname,
                    Status = s.Status,
                    ActiveTeamId = assignment?.Marketingteamid,
                    ActiveTeamCode = assignment?.Teamcode,
                    IsLeaderInActiveTeam = assignment?.IsLeader ?? false
                };
            }).ToList();
        }

        /// <summary>
        /// Inserts a new salesman record.
        /// </summary>
        public async Task<Salesman> CreateAsync(Salesman salesman)
        {
            _dbContext.Salesmen.Add(salesman);
            await _dbContext.SaveChangesAsync();
            return salesman;
        }

        /// <summary>
        /// Updates an existing salesman record.
        /// </summary>
        public async Task UpdateAsync(Salesman salesman)
        {
            _dbContext.Salesmen.Update(salesman);
            await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Deletes a salesman record.
        /// </summary>
        public async Task DeleteAsync(Salesman salesman)
        {
            _dbContext.Salesmen.Remove(salesman);
            await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Checks if a salesman is assigned to any team (as team leader or team detail row).
        /// Used for Rule R2: Salesman cannot be deleted if assigned to a team.
        /// </summary>
        public async Task<bool> IsAssignedToAnyTeamAsync(long salesmanId)
        {
            var isLeader = await _dbContext.Marketingteams.AnyAsync(t => t.Teamleaderid == salesmanId);
            if (isLeader) return true;

            var isDetailMember = await _dbContext.Marketingteamdetails.AnyAsync(d => d.Salesmanid == salesmanId);
            return isDetailMember;
        }

        /// <summary>
        /// Gets a list of salesmen by their IDs (used for validation).
        /// </summary>
        public async Task<List<Salesman>> GetSalesmenByIdsAsync(IEnumerable<long> ids)
        {
            return await _dbContext.Salesmen
                .AsNoTracking()
                .Where(s => ids.Contains(s.Id))
                .ToListAsync();
        }

        /// <summary>
        /// Gets the active marketing team assignment for a salesman (as leader or member).
        /// </summary>
        public async Task<ActiveTeamAssignmentDto?> GetActiveTeamAssignmentAsync(long salesmanId)
        {
            // Check if leader of active team
            var leaderTeam = await _dbContext.Marketingteams
                .AsNoTracking()
                .Where(t => t.Teamleaderid == salesmanId && t.Status == 1)
                .Select(t => new ActiveTeamAssignmentDto
                {
                    TeamId = t.Id,
                    TeamCode = t.Teamcode,
                    TeamName = t.Teamname,
                    RoleInTeam = "Leader"
                })
                .FirstOrDefaultAsync();

            if (leaderTeam != null) return leaderTeam;

            // Check if member of active team
            var memberTeam = await _dbContext.Marketingteamdetails
                .AsNoTracking()
                .Include(d => d.Marketingteam)
                .Where(d => d.Salesmanid == salesmanId && d.Status == 1 && d.Marketingteam.Status == 1)
                .Select(d => new ActiveTeamAssignmentDto
                {
                    TeamId = d.Marketingteamid,
                    TeamCode = d.Marketingteam.Teamcode,
                    TeamName = d.Marketingteam.Teamname,
                    RoleInTeam = d.Role == 1 ? "Leader" : "Member"
                })
                .FirstOrDefaultAsync();

            return memberTeam;
        }

        public async Task<SalesmanDeactivationRequest> CreateDeactivationRequestAsync(SalesmanDeactivationRequest request)
        {
            _dbContext.SalesmanDeactivationRequests.Add(request);
            await _dbContext.SaveChangesAsync();
            return request;
        }

        public async Task<List<SalesmanDeactivationRequest>> GetPendingDeactivationRequestsAsync()
        {
            return await _dbContext.SalesmanDeactivationRequests
                .AsNoTracking()
                .Where(r => r.Status == "Pending")
                .OrderByDescending(r => r.Requesteddate)
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
            return await _dbContext.SalesmanDeactivationRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Salesmanid == salesmanId && r.Status == "Pending");
        }

        public async Task DeactivateTeamAndMembersAsync(long teamId)
        {
            var team = await _dbContext.Marketingteams.FirstOrDefaultAsync(t => t.Id == teamId);
            if (team != null)
            {
                team.Status = 2; // Inactive
                team.Modifieddate = DateTime.UtcNow;
            }

            var details = await _dbContext.Marketingteamdetails
                .Where(d => d.Marketingteamid == teamId)
                .ToListAsync();

            foreach (var d in details)
            {
                d.Status = 2; // Inactive
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task DeactivateTeamMemberAsync(long teamId, long salesmanId)
        {
            var details = await _dbContext.Marketingteamdetails
                .Where(d => d.Marketingteamid == teamId && d.Salesmanid == salesmanId)
                .ToListAsync();

            foreach (var d in details)
            {
                d.Status = 2; // Inactive
            }

            var team = await _dbContext.Marketingteams.FirstOrDefaultAsync(t => t.Id == teamId);
            if (team != null)
            {
                team.Modifieddate = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}
