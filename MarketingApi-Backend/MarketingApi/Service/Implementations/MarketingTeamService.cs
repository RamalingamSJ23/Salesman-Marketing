using FluentValidation;
using MarketingApi.Common;
using MarketingApi.DTOs.Common;
using MarketingApi.DTOs.MarketingTeam;
using MarketingApi.Enums;
using MarketingApi.Models.Entities;
using MarketingApi.Repository.Interfaces;
using MarketingApi.Service.Interfaces;

namespace MarketingApi.Service.Implementations;

/// <summary>
/// Implements business logic and validation rules (R3 to R10) for Marketing Team Master.
/// </summary>
public class MarketingTeamService : IMarketingTeamService
{
    private readonly IMarketingTeamRepository _marketingTeamRepository;
    private readonly ISalesmanRepository _salesmanRepository;
    private readonly IValidator<CreateMarketingTeamDto> _createValidator;
    private readonly IValidator<UpdateMarketingTeamDto> _updateValidator;
    private readonly ILogger<MarketingTeamService> _logger;

    public MarketingTeamService(
        IMarketingTeamRepository marketingTeamRepository,
        ISalesmanRepository salesmanRepository,
        IValidator<CreateMarketingTeamDto> createValidator,
        IValidator<UpdateMarketingTeamDto> updateValidator,
        ILogger<MarketingTeamService> logger)
    {
        _marketingTeamRepository = marketingTeamRepository;
        _salesmanRepository = salesmanRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    public async Task<ServiceResult<PagedResultDto<MarketingTeamListDto>>> GetPagedTeamsAsync(MarketingTeamQueryParameters parameters)
    {
        parameters.PageNumber = Math.Max(1, parameters.PageNumber);
        parameters.PageSize = Math.Clamp(parameters.PageSize <= 0 ? 10 : parameters.PageSize, 1, 100);

        var result = await _marketingTeamRepository.GetPagedTeamsAsync(parameters);
        return ServiceResult<PagedResultDto<MarketingTeamListDto>>.Ok(result);
    }

    public async Task<ServiceResult<MarketingTeamDto>> GetByIdAsync(long id)
    {
        var team = await _marketingTeamRepository.GetByIdWithDetailsAsync(id);
        if (team == null)
        {
            return ServiceResult<MarketingTeamDto>.NotFound($"Marketing team with ID {id} was not found.", "TEAM_NOT_FOUND");
        }

        return ServiceResult<MarketingTeamDto>.Ok(MapToDto(team));
    }

    public async Task<ServiceResult<MarketingTeamDto>> CreateTeamAsync(CreateMarketingTeamDto dto)
    {
        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            var failure = validation.Errors[0];
            _logger.LogWarning("Team creation validation failed on {Field}: {Error}", failure.PropertyName, failure.ErrorMessage);
            return ServiceResult<MarketingTeamDto>.BadRequest(failure.ErrorMessage, failure.PropertyName.ToLowerInvariant(), "VALIDATION_ERROR");
        }

        var cleanCode = dto.Teamcode.Trim().ToUpperInvariant();
        if (await _marketingTeamRepository.CodeExistsAsync(cleanCode))
        {
            return ServiceResult<MarketingTeamDto>.Conflict($"Team code {cleanCode} already exists.", "teamcode", "DUPLICATE_TEAM_CODE");
        }

        var members = dto.Members ?? new List<MarketingTeamDetailInputDto>();
        var validationError = await ValidateTeamMembersAsync(
            teamId: null,
            isTeamActive: true,
            teamLeaderId: dto.Teamleaderid,
            members: members);

        if (validationError != null)
        {
            return ServiceResult<MarketingTeamDto>.BadRequest(validationError, "members", "TEAM_RULE_VIOLATION");
        }

        var team = new MarketingTeam
        {
            TeamCode = cleanCode,
            TeamName = dto.Teamname.Trim(),
            TeamLeaderId = dto.Teamleaderid,
            Description = dto.Description?.Trim(),
            Status = (short)RecordStatus.Active
        };

        var details = members.Select(m => new MarketingTeamDetail
        {
            SalesmanId = m.Salesmanid,
            Role = m.Role,
            TargetAmount = m.Targetamount,
            Status = m.Status
        }).ToList();

        var created = await _marketingTeamRepository.CreateTeamWithMembersAsync(team, details);
        _logger.LogInformation("Marketing team {TeamId} ({TeamCode}) created with {MemberCount} members.", created.Id, created.TeamCode, details.Count);

        var savedTeam = await _marketingTeamRepository.GetByIdWithDetailsAsync(created.Id);
        return ServiceResult<MarketingTeamDto>.Created(MapToDto(savedTeam!));
    }

    public async Task<ServiceResult<MarketingTeamDto>> AddSalesmanToTeamAsync(long id, MarketingTeamDetailInputDto dto)
    {
        var existingTeam = await _marketingTeamRepository.GetByIdWithDetailsAsync(id);
        if (existingTeam == null)
        {
            return ServiceResult<MarketingTeamDto>.NotFound($"Marketing team with ID {id} was not found.", "TEAM_NOT_FOUND");
        }

        if (existingTeam.Status == (short)RecordStatus.Inactive)
        {
            return ServiceResult<MarketingTeamDto>.BadRequest(
                $"Team {existingTeam.TeamCode} is inactive. Activate the team before adding a salesman.",
                code: "TEAM_INACTIVE");
        }

        if (dto.Targetamount < 0)
        {
            return ServiceResult<MarketingTeamDto>.BadRequest("Target amount cannot be negative.");
        }

        var activeMembers = existingTeam.MarketingTeamDetails
            .Where(d => d.Status == (short)RecordStatus.Active)
            .ToList();

        if (activeMembers.Any(d => d.SalesmanId == dto.Salesmanid))
        {
            var existingSalesman = await _salesmanRepository.GetByIdAsync(dto.Salesmanid);
            var name = existingSalesman?.SalesmanName ?? $"ID {dto.Salesmanid}";
            return ServiceResult<MarketingTeamDto>.BadRequest($"Salesman {name} is already a member of this team.");
        }

        var salesman = await _salesmanRepository.GetByIdAsync(dto.Salesmanid);
        if (salesman == null)
        {
            return ServiceResult<MarketingTeamDto>.BadRequest($"Salesman with ID {dto.Salesmanid} does not exist.");
        }

        if (salesman.Status != (short)RecordStatus.Active)
        {
            return ServiceResult<MarketingTeamDto>.BadRequest($"Salesman {salesman.SalesmanName} is inactive.");
        }

        if (dto.Status == (short)RecordStatus.Active)
        {
            var activeTeamCode = await _marketingTeamRepository.GetActiveTeamCodeForSalesmanAsync(dto.Salesmanid, id);
            if (!string.IsNullOrEmpty(activeTeamCode))
            {
                return ServiceResult<MarketingTeamDto>.BadRequest(
                    $"{salesman.SalesmanName} is already in active team {activeTeamCode}.");
            }
        }

        var hasMembers = activeMembers.Any();
        if (!hasMembers &&
            (dto.Role != (short)SalesmanRole.TeamLeader || dto.Salesmanid != existingTeam.TeamLeaderId))
        {
            return ServiceResult<MarketingTeamDto>.BadRequest(
                "The first salesman added to a team must be its Team Leader.");
        }

        if (hasMembers && dto.Role == (short)SalesmanRole.TeamLeader)
        {
            return ServiceResult<MarketingTeamDto>.BadRequest(
                "The Team Leader is already assigned to this team.");
        }

        await _marketingTeamRepository.AddTeamMemberAsync(new MarketingTeamDetail
        {
            MarketingTeamId = id,
            SalesmanId = dto.Salesmanid,
            Role = dto.Role,
            TargetAmount = dto.Targetamount,
            Status = dto.Status
        });

        _logger.LogInformation("Salesman {SalesmanId} added to team {TeamId} ({TeamCode}).", dto.Salesmanid, id, existingTeam.TeamCode);
        var updatedTeam = await _marketingTeamRepository.GetByIdWithDetailsAsync(id);
        return ServiceResult<MarketingTeamDto>.Ok(MapToDto(updatedTeam!));
    }

    public async Task<ServiceResult<MarketingTeamDto>> UpdateTeamAsync(long id, UpdateMarketingTeamDto dto)
    {
        var existingTeam = await _marketingTeamRepository.GetByIdWithDetailsAsync(id);
        if (existingTeam == null)
        {
            return ServiceResult<MarketingTeamDto>.NotFound($"Marketing team with ID {id} was not found.", "TEAM_NOT_FOUND");
        }

        if (existingTeam.Status == (short)RecordStatus.Inactive)
        {
            return ServiceResult<MarketingTeamDto>.BadRequest(
                $"Team {existingTeam.TeamCode} is inactive. Activate the team before editing its members.",
                code: "TEAM_INACTIVE");
        }

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            var failure = validation.Errors[0];
            _logger.LogWarning("Team {TeamId} update validation failed on {Field}: {Error}", id, failure.PropertyName, failure.ErrorMessage);
            return ServiceResult<MarketingTeamDto>.BadRequest(failure.ErrorMessage, failure.PropertyName.ToLowerInvariant(), "VALIDATION_ERROR");
        }

        var members = dto.Members ?? new List<MarketingTeamDetailInputDto>();
        var existingByRowId = existingTeam.MarketingTeamDetails.ToDictionary(d => d.Id);

        // Disallow changing the salesman on an existing detail row; require remove + add instead (H13)
        foreach (var member in members.Where(m => m.Id.HasValue && m.Id.Value > 0))
        {
            if (existingByRowId.TryGetValue(member.Id!.Value, out var existingRow) &&
                existingRow.SalesmanId != member.Salesmanid)
            {
                return ServiceResult<MarketingTeamDto>.BadRequest(
                    "Cannot change salesman on an existing member row. Remove the row and add the new salesman instead.",
                    "members",
                    "MEMBER_SALESMAN_IMMUTABLE");
            }
        }

        // Only compare newly added rows against kept rows (rows not being removed in this save) (H13)
        var keptExistingIds = members
            .Where(m => m.Id.HasValue && m.Id.Value > 0)
            .Select(m => m.Id!.Value)
            .ToHashSet();

        var keptSalesmanIds = existingTeam.MarketingTeamDetails
            .Where(d => keptExistingIds.Contains(d.Id))
            .Select(d => d.SalesmanId)
            .ToHashSet();

        var newMembers = members.Where(m => !m.Id.HasValue || m.Id.Value == 0).ToList();
        foreach (var newMember in newMembers)
        {
            if (keptSalesmanIds.Contains(newMember.Salesmanid))
            {
                var sm = await _salesmanRepository.GetByIdAsync(newMember.Salesmanid);
                var smName = sm?.SalesmanName ?? $"ID {newMember.Salesmanid}";
                return ServiceResult<MarketingTeamDto>.BadRequest(
                    $"Salesman {smName} is already a member of this team.",
                    "members",
                    "DUPLICATE_TEAM_MEMBER");
            }
        }

        var validationError = await ValidateTeamMembersAsync(
            teamId: id,
            isTeamActive: true,
            teamLeaderId: dto.Teamleaderid,
            members: members);

        if (validationError != null)
        {
            return ServiceResult<MarketingTeamDto>.BadRequest(validationError, "members", "TEAM_RULE_VIOLATION");
        }

        await _marketingTeamRepository.UpdateTeamWithMembersAsync(id, dto);
        _logger.LogInformation("Marketing team {TeamId} ({TeamCode}) updated.", id, existingTeam.TeamCode);

        var refreshed = await _marketingTeamRepository.GetByIdWithDetailsAsync(id);
        return ServiceResult<MarketingTeamDto>.Ok(MapToDto(refreshed!));
    }

    public async Task<ServiceResult<MarketingTeamDto>> DeactivateTeamAsync(long id)
    {
        var existingTeam = await _marketingTeamRepository.GetByIdWithDetailsAsync(id);
        if (existingTeam == null)
        {
            return ServiceResult<MarketingTeamDto>.NotFound($"Marketing team with ID {id} was not found.", "TEAM_NOT_FOUND");
        }

        if (existingTeam.Status == (short)RecordStatus.Inactive)
        {
            return ServiceResult<MarketingTeamDto>.BadRequest($"Marketing team {existingTeam.TeamCode} is already inactive.", code: "ALREADY_INACTIVE");
        }

        await _marketingTeamRepository.DeactivateTeamWithMembersAsync(id);
        _logger.LogInformation("Marketing team {TeamId} ({TeamCode}) and all members deactivated.", id, existingTeam.TeamCode);

        var updated = await _marketingTeamRepository.GetByIdWithDetailsAsync(id);
        return ServiceResult<MarketingTeamDto>.Ok(MapToDto(updated!));
    }

    /// <summary>
    /// Validates Team Leader eligibility (Rules R7 and R8) and activates the team header.
    /// Member rows remain Inactive until updated via team edit.
    /// </summary>
    public async Task<ServiceResult<MarketingTeamDto>> ActivateTeamAsync(long id)
    {
        var existingTeam = await _marketingTeamRepository.GetByIdWithDetailsAsync(id);
        if (existingTeam == null)
        {
            return ServiceResult<MarketingTeamDto>.NotFound($"Marketing team with ID {id} was not found.", "TEAM_NOT_FOUND");
        }

        if (existingTeam.Status == (short)RecordStatus.Active)
        {
            return ServiceResult<MarketingTeamDto>.BadRequest($"Marketing team {existingTeam.TeamCode} is already active.", code: "ALREADY_ACTIVE");
        }

        // Rule R7: Team leader must still exist and be Active
        var leader = await _salesmanRepository.GetByIdAsync(existingTeam.TeamLeaderId);
        if (leader == null || leader.Status != (short)RecordStatus.Active)
        {
            var leaderLabel = leader?.SalesmanName ?? $"ID {existingTeam.TeamLeaderId}";
            return ServiceResult<MarketingTeamDto>.BadRequest(
                $"Cannot activate team {existingTeam.TeamCode}: Team Leader {leaderLabel} is inactive or missing.",
                code: "LEADER_INACTIVE");
        }

        // Rule R8: Team leader cannot already belong to another active team
        var conflictingTeamCode = await _marketingTeamRepository.GetActiveTeamCodeForSalesmanAsync(existingTeam.TeamLeaderId, id);
        if (!string.IsNullOrEmpty(conflictingTeamCode))
        {
            return ServiceResult<MarketingTeamDto>.BadRequest(
                $"Cannot activate team {existingTeam.TeamCode}: Team Leader {leader.SalesmanName} is already assigned to active team {conflictingTeamCode}.",
                code: "LEADER_ALREADY_ASSIGNED");
        }

        await _marketingTeamRepository.ActivateTeamAsync(id);
        _logger.LogInformation("Marketing team {TeamId} ({TeamCode}) activated.", id, existingTeam.TeamCode);

        var updated = await _marketingTeamRepository.GetByIdWithDetailsAsync(id);
        return ServiceResult<MarketingTeamDto>.Ok(MapToDto(updated!));
    }

    public List<EnumOptionDto> GetRoleOptions()
    {
        return new List<EnumOptionDto>
        {
            new() { Value = (short)SalesmanRole.TeamLeader, Name = nameof(SalesmanRole.TeamLeader), DisplayText = "Team Leader" },
            new() { Value = (short)SalesmanRole.SeniorSalesExecutive, Name = nameof(SalesmanRole.SeniorSalesExecutive), DisplayText = "Senior Sales Executive" },
            new() { Value = (short)SalesmanRole.SalesExecutive, Name = nameof(SalesmanRole.SalesExecutive), DisplayText = "Sales Executive" },
            new() { Value = (short)SalesmanRole.Trainee, Name = nameof(SalesmanRole.Trainee), DisplayText = "Trainee" }
        };
    }

    private async Task<string?> ValidateTeamMembersAsync(
        long? teamId,
        bool isTeamActive,
        long teamLeaderId,
        List<MarketingTeamDetailInputDto> members,
        bool allowEmptyMembers = false)
    {
        // Rule R4: A team must have at least one member row
        if (!allowEmptyMembers && (members == null || members.Count == 0))
        {
            return "Add at least one salesman to the team.";
        }

        if (members == null || members.Count == 0)
        {
            return null;
        }

        // Rule R9: Target amount is 0 or more
        if (members.Any(m => m.Targetamount < 0))
        {
            return "Target amount cannot be negative.";
        }

        var salesmanIds = members.Select(m => m.Salesmanid).Distinct().ToList();
        var salesmen = await _salesmanRepository.GetSalesmenByIdsAsync(salesmanIds);
        var salesmanDict = salesmen.ToDictionary(s => s.Id);

        // Rule R5: The same salesman cannot appear twice in one team
        var duplicateId = members
            .GroupBy(m => m.Salesmanid)
            .FirstOrDefault(g => g.Count() > 1)?.Key;

        if (duplicateId.HasValue)
        {
            var duplicateName = salesmanDict.TryGetValue(duplicateId.Value, out var s) ? s.SalesmanName : $"ID {duplicateId.Value}";
            return $"Salesman {duplicateName} is already in this team.";
        }

        // Rule R6: Exactly one row has role Team Leader, matching header Team Leader
        var leaderRows = members
            .Where(m => m.Role == (short)SalesmanRole.TeamLeader)
            .ToList();

        if (leaderRows.Count != 1 || leaderRows[0].Salesmanid != teamLeaderId)
        {
            return "The Team Leader in the header must be the only member with role Team Leader.";
        }

        // Rule R7: Only Active salesmen can be assigned
        foreach (var member in members)
        {
            if (salesmanDict.TryGetValue(member.Salesmanid, out var salesman))
            {
                if (salesman.Status != (short)RecordStatus.Active)
                {
                    return $"Salesman {salesman.SalesmanName} is inactive.";
                }
            }
            else
            {
                return $"Salesman with ID {member.Salesmanid} does not exist.";
            }
        }

        // Rule R8: Batch-check active team memberships in a single query (M6)
        if (isTeamActive)
        {
            var activeMemberIds = members
                .Where(m => m.Status == (short)RecordStatus.Active)
                .Select(m => m.Salesmanid)
                .Distinct()
                .ToList();

            var conflictingTeams = await _marketingTeamRepository.GetActiveTeamCodesForSalesmenAsync(activeMemberIds, teamId);
            foreach (var memberId in activeMemberIds)
            {
                if (conflictingTeams.TryGetValue(memberId, out var activeTeamCode))
                {
                    var sName = salesmanDict.TryGetValue(memberId, out var sm) ? sm.SalesmanName : "Salesman";
                    return $"{sName} is already in active team {activeTeamCode}.";
                }
            }
        }

        return null;
    }

    private static MarketingTeamDto MapToDto(MarketingTeam team)
    {
        var visibleDetails = team.Status == (short)RecordStatus.Active
            ? team.MarketingTeamDetails.Where(d => d.Status == (short)RecordStatus.Active)
            : team.MarketingTeamDetails;

        return new MarketingTeamDto
        {
            Id = team.Id,
            Teamcode = team.TeamCode,
            Teamname = team.TeamName,
            Teamleaderid = team.TeamLeaderId,
            Teamleadername = team.TeamLeader?.SalesmanName ?? string.Empty,
            Description = team.Description,
            Status = team.Status,
            Createddate = team.CreatedDate,
            Modifieddate = team.ModifiedDate,
            Members = visibleDetails.Select(d => new MarketingTeamDetailDto
            {
                Id = d.Id,
                Salesmanid = d.SalesmanId,
                Salesmancode = d.Salesman?.SalesmanCode ?? string.Empty,
                Salesmanname = d.Salesman?.SalesmanName ?? string.Empty,
                Role = d.Role,
                RoleName = GetRoleDisplayName(d.Role),
                Targetamount = d.TargetAmount,
                Status = d.Status
            }).ToList()
        };
    }

    private static string GetRoleDisplayName(short role)
    {
        return role switch
        {
            (short)SalesmanRole.TeamLeader => "Team Leader",
            (short)SalesmanRole.SeniorSalesExecutive => "Senior Sales Executive",
            (short)SalesmanRole.SalesExecutive => "Sales Executive",
            (short)SalesmanRole.Trainee => "Trainee",
            _ => "Unknown"
        };
    }
}
