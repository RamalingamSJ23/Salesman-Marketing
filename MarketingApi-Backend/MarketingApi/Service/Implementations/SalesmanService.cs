using FluentValidation;
using MarketingApi.Common;
using MarketingApi.DTOs.Common;
using MarketingApi.DTOs.Salesman;
using MarketingApi.Enums;
using MarketingApi.Models.Entities;
using MarketingApi.Repository.Interfaces;
using MarketingApi.Service.Interfaces;

namespace MarketingApi.Service.Implementations;

/// <summary>
/// Implements business logic and validation rules for Salesman Master.
/// </summary>
public class SalesmanService : ISalesmanService
{
    private readonly ISalesmanRepository _salesmanRepository;
    private readonly IValidator<CreateSalesmanDto> _createValidator;
    private readonly IValidator<UpdateSalesmanDto> _updateValidator;
    private readonly ILogger<SalesmanService> _logger;

    public SalesmanService(
        ISalesmanRepository salesmanRepository,
        IValidator<CreateSalesmanDto> createValidator,
        IValidator<UpdateSalesmanDto> updateValidator,
        ILogger<SalesmanService> logger)
    {
        _salesmanRepository = salesmanRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    public async Task<ServiceResult<PagedResultDto<SalesmanListDto>>> GetPagedSalesmenAsync(SalesmanQueryParameters parameters)
    {
        parameters.PageNumber = Math.Max(1, parameters.PageNumber);
        parameters.PageSize = Math.Clamp(parameters.PageSize <= 0 ? 10 : parameters.PageSize, 1, 100);

        var result = await _salesmanRepository.GetPagedSalesmenAsync(parameters);
        return ServiceResult<PagedResultDto<SalesmanListDto>>.Ok(result);
    }

    public async Task<ServiceResult<SalesmanDto>> GetByIdAsync(long id)
    {
        var salesman = await _salesmanRepository.GetByIdAsync(id);
        if (salesman == null)
        {
            return ServiceResult<SalesmanDto>.NotFound($"Salesman with ID {id} was not found.", "SALESMAN_NOT_FOUND");
        }

        return ServiceResult<SalesmanDto>.Ok(MapToDto(salesman));
    }

    /// <summary>
    /// Creates a new salesman (Rule R1: unique code, unique mobile/email, auto-normalized).
    /// </summary>
    public async Task<ServiceResult<SalesmanDto>> CreateSalesmanAsync(CreateSalesmanDto dto)
    {
        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            var failure = validation.Errors[0];
            _logger.LogWarning("Salesman creation validation failed on {Field}: {Error}", failure.PropertyName, failure.ErrorMessage);
            return ServiceResult<SalesmanDto>.BadRequest(failure.ErrorMessage, failure.PropertyName.ToLowerInvariant(), "VALIDATION_ERROR");
        }

        var cleanCode = dto.Salesmancode.Trim().ToUpperInvariant();
        if (await _salesmanRepository.CodeExistsAsync(cleanCode))
        {
            return ServiceResult<SalesmanDto>.Conflict($"Salesman code {cleanCode} already exists.", "salesmancode", "DUPLICATE_SALESMAN_CODE");
        }

        var cleanMobile = dto.Mobileno.Trim();
        if (await _salesmanRepository.MobileExistsAsync(cleanMobile))
        {
            return ServiceResult<SalesmanDto>.Conflict("Mobile number already exists.", "mobileno", "DUPLICATE_MOBILE");
        }

        var cleanEmail = dto.Email.Trim().ToLowerInvariant();
        if (await _salesmanRepository.EmailExistsAsync(cleanEmail))
        {
            return ServiceResult<SalesmanDto>.Conflict("Email address already exists.", "email", "DUPLICATE_EMAIL");
        }

        var salesman = new Salesman
        {
            SalesmanCode = cleanCode,
            SalesmanName = dto.Salesmanname.Trim(),
            MobileNo = cleanMobile,
            Email = cleanEmail,
            JoiningDate = dto.Joiningdate,
            Status = dto.Status == (short)RecordStatus.Inactive ? (short)RecordStatus.Inactive : (short)RecordStatus.Active
        };

        var created = await _salesmanRepository.CreateAsync(salesman);
        _logger.LogInformation("Salesman {SalesmanId} ({SalesmanCode}) created.", created.Id, created.SalesmanCode);
        return ServiceResult<SalesmanDto>.Created(MapToDto(created));
    }

    public async Task<ServiceResult<SalesmanDto>> UpdateSalesmanAsync(long id, UpdateSalesmanDto dto)
    {
        var salesman = await _salesmanRepository.GetByIdAsync(id, trackChanges: true);
        if (salesman == null)
        {
            return ServiceResult<SalesmanDto>.NotFound($"Salesman with ID {id} was not found.", "SALESMAN_NOT_FOUND");
        }

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            var failure = validation.Errors[0];
            _logger.LogWarning("Salesman {SalesmanId} update validation failed on {Field}: {Error}", id, failure.PropertyName, failure.ErrorMessage);
            return ServiceResult<SalesmanDto>.BadRequest(failure.ErrorMessage, failure.PropertyName.ToLowerInvariant(), "VALIDATION_ERROR");
        }

        var cleanMobile = dto.Mobileno.Trim();
        if (await _salesmanRepository.MobileExistsAsync(cleanMobile, id))
        {
            return ServiceResult<SalesmanDto>.Conflict("Mobile number already exists.", "mobileno", "DUPLICATE_MOBILE");
        }

        var cleanEmail = dto.Email.Trim().ToLowerInvariant();
        if (await _salesmanRepository.EmailExistsAsync(cleanEmail, id))
        {
            return ServiceResult<SalesmanDto>.Conflict("Email address already exists.", "email", "DUPLICATE_EMAIL");
        }

        salesman.SalesmanName = dto.Salesmanname.Trim();
        salesman.MobileNo = cleanMobile;
        salesman.Email = cleanEmail;
        salesman.JoiningDate = dto.Joiningdate;

        await _salesmanRepository.UpdateAsync(salesman);
        _logger.LogInformation("Salesman {SalesmanId} ({SalesmanCode}) updated.", salesman.Id, salesman.SalesmanCode);
        return ServiceResult<SalesmanDto>.Ok(MapToDto(salesman));
    }

    public async Task<ServiceResult<ActiveTeamAssignmentDto?>> CheckSalesmanTeamAssignmentAsync(long salesmanId)
    {
        var assignment = await _salesmanRepository.GetActiveTeamAssignmentAsync(salesmanId);
        return ServiceResult<ActiveTeamAssignmentDto?>.Ok(assignment);
    }

    public async Task<ServiceResult<DeactivateSalesmanResultDto>> DeactivateSalesmanAsync(long id, string requestedBy)
    {
        var salesman = await _salesmanRepository.GetByIdAsync(id, trackChanges: true);
        if (salesman == null)
        {
            return ServiceResult<DeactivateSalesmanResultDto>.NotFound($"Salesman with ID {id} was not found.", "SALESMAN_NOT_FOUND");
        }

        if (salesman.Status == (short)RecordStatus.Inactive)
        {
            return ServiceResult<DeactivateSalesmanResultDto>.BadRequest(
                $"Salesman {salesman.SalesmanCode} is already inactive.",
                code: "ALREADY_INACTIVE");
        }

        var existingPending = await _salesmanRepository.GetPendingRequestForSalesmanAsync(id);
        if (existingPending != null)
        {
            return ServiceResult<DeactivateSalesmanResultDto>.BadRequest(
                $"A deactivation request for salesman {salesman.SalesmanCode} is already pending approval from Marketing Master.",
                code: "DEACTIVATION_ALREADY_PENDING");
        }

        var assignment = await _salesmanRepository.GetActiveTeamAssignmentAsync(id);
        if (assignment != null)
        {
            var request = new SalesmanDeactivationRequest
            {
                SalesmanId = salesman.Id,
                SalesmanCode = salesman.SalesmanCode,
                SalesmanName = salesman.SalesmanName,
                TeamId = assignment.TeamId,
                TeamCode = assignment.TeamCode,
                TeamName = assignment.TeamName,
                RoleInTeam = assignment.RoleInTeam,
                Reason = $"Salesman deactivation requested while assigned as {assignment.RoleInTeam} in Team {assignment.TeamCode}.",
                RequestedBy = requestedBy,
                Status = nameof(DeactivationRequestStatus.Pending),
                RequestedDate = DateTime.UtcNow
            };

            var createdRequest = await _salesmanRepository.CreateDeactivationRequestAsync(request);
            _logger.LogInformation(
                "Deactivation request {RequestId} created for salesman {SalesmanId} ({RoleInTeam} in {TeamCode}) by {User}.",
                createdRequest.Id, salesman.Id, assignment.RoleInTeam, assignment.TeamCode, requestedBy);

            return ServiceResult<DeactivateSalesmanResultDto>.Ok(new DeactivateSalesmanResultDto
            {
                DirectDeactivation = false,
                RequestSentToMarketingMaster = true,
                Message = $"Deactivation request submitted to Marketing Master for approval. Salesman {salesman.SalesmanCode} is currently {assignment.RoleInTeam} in Team {assignment.TeamCode}.",
                Salesman = MapToDto(salesman),
                DeactivationRequest = MapToRequestDto(createdRequest)
            });
        }

        salesman.Status = (short)RecordStatus.Inactive;
        await _salesmanRepository.UpdateAsync(salesman);
        _logger.LogInformation("Salesman {SalesmanId} ({SalesmanCode}) deactivated directly by {User}.", salesman.Id, salesman.SalesmanCode, requestedBy);

        return ServiceResult<DeactivateSalesmanResultDto>.Ok(new DeactivateSalesmanResultDto
        {
            DirectDeactivation = true,
            RequestSentToMarketingMaster = false,
            Message = $"Salesman {salesman.SalesmanCode} deactivated successfully.",
            Salesman = MapToDto(salesman)
        });
    }

    public async Task<ServiceResult<List<SalesmanDeactivationRequestDto>>> GetPendingDeactivationRequestsAsync()
    {
        var list = await _salesmanRepository.GetPendingDeactivationRequestsAsync();
        return ServiceResult<List<SalesmanDeactivationRequestDto>>.Ok(list.Select(MapToRequestDto).ToList());
    }

    public async Task<ServiceResult<SalesmanDeactivationRequestDto>> ApproveDeactivationRequestAsync(long requestId, string processedBy)
    {
        var request = await _salesmanRepository.GetDeactivationRequestByIdAsync(requestId);
        if (request == null)
        {
            return ServiceResult<SalesmanDeactivationRequestDto>.NotFound($"Deactivation request with ID {requestId} was not found.");
        }

        if (!string.Equals(request.Status, nameof(DeactivationRequestStatus.Pending), StringComparison.OrdinalIgnoreCase))
        {
            return ServiceResult<SalesmanDeactivationRequestDto>.BadRequest($"Request is already {request.Status}.", code: "INVALID_REQUEST_STATE");
        }

        var salesman = await _salesmanRepository.GetByIdAsync(request.SalesmanId, trackChanges: true);
        if (salesman == null)
        {
            return ServiceResult<SalesmanDeactivationRequestDto>.NotFound($"Salesman with ID {request.SalesmanId} was not found.");
        }

        // Re-evaluate current active assignment at approval time rather than trusting stale snapshot (H5)
        var currentAssignment = await _salesmanRepository.GetActiveTeamAssignmentAsync(request.SalesmanId);

        var approved = await _salesmanRepository.ApproveDeactivationAtomicallyAsync(
            request,
            salesman,
            currentAssignment,
            processedBy);

        _logger.LogInformation(
            "Deactivation request {RequestId} for salesman {SalesmanId} approved by {User} (current role: {Role}).",
            requestId, salesman.Id, processedBy, currentAssignment?.RoleInTeam ?? "None");

        return ServiceResult<SalesmanDeactivationRequestDto>.Ok(MapToRequestDto(approved));
    }

    public async Task<ServiceResult<SalesmanDeactivationRequestDto>> RejectDeactivationRequestAsync(long requestId, string processedBy, string? notes = null)
    {
        var request = await _salesmanRepository.GetDeactivationRequestByIdAsync(requestId);
        if (request == null)
        {
            return ServiceResult<SalesmanDeactivationRequestDto>.NotFound($"Deactivation request with ID {requestId} was not found.");
        }

        if (!string.Equals(request.Status, nameof(DeactivationRequestStatus.Pending), StringComparison.OrdinalIgnoreCase))
        {
            return ServiceResult<SalesmanDeactivationRequestDto>.BadRequest($"Request is already {request.Status}.", code: "INVALID_REQUEST_STATE");
        }

        request.Status = nameof(DeactivationRequestStatus.Rejected);
        request.ProcessedDate = DateTime.UtcNow;
        request.ProcessedBy = processedBy;
        request.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        await _salesmanRepository.UpdateDeactivationRequestAsync(request);

        _logger.LogInformation("Deactivation request {RequestId} for salesman {SalesmanId} rejected by {User}.", requestId, request.SalesmanId, processedBy);
        return ServiceResult<SalesmanDeactivationRequestDto>.Ok(MapToRequestDto(request));
    }

    public async Task<ServiceResult<SalesmanDto>> ActivateSalesmanAsync(long id, string processedBy)
    {
        var salesman = await _salesmanRepository.GetByIdAsync(id, trackChanges: true);
        if (salesman == null)
        {
            return ServiceResult<SalesmanDto>.NotFound($"Salesman with ID {id} was not found.", "SALESMAN_NOT_FOUND");
        }

        if (salesman.Status == (short)RecordStatus.Active)
        {
            return ServiceResult<SalesmanDto>.BadRequest($"Salesman {salesman.SalesmanCode} is already active.", code: "ALREADY_ACTIVE");
        }

        await _salesmanRepository.ActivateSalesmanAndCancelPendingRequestsAsync(salesman, processedBy);
        _logger.LogInformation("Salesman {SalesmanId} ({SalesmanCode}) activated by {User}.", salesman.Id, salesman.SalesmanCode, processedBy);

        return ServiceResult<SalesmanDto>.Ok(MapToDto(salesman));
    }

    public async Task<ServiceResult<List<SalesmanLookupDto>>> GetActiveSalesmenLookupAsync()
    {
        var lookup = await _salesmanRepository.GetActiveSalesmenLookupAsync();
        return ServiceResult<List<SalesmanLookupDto>>.Ok(lookup);
    }

    private static SalesmanDto MapToDto(Salesman s)
    {
        return new SalesmanDto
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
        };
    }

    private static SalesmanDeactivationRequestDto MapToRequestDto(SalesmanDeactivationRequest r)
    {
        return new SalesmanDeactivationRequestDto
        {
            Id = r.Id,
            SalesmanId = r.SalesmanId,
            SalesmanCode = r.SalesmanCode,
            SalesmanName = r.SalesmanName,
            TeamId = r.TeamId,
            TeamCode = r.TeamCode,
            TeamName = r.TeamName,
            RoleInTeam = r.RoleInTeam,
            Reason = r.Reason,
            RequestedBy = r.RequestedBy,
            Status = r.Status,
            RequestedDate = r.RequestedDate,
            ProcessedDate = r.ProcessedDate,
            ProcessedBy = r.ProcessedBy,
            Notes = r.Notes
        };
    }
}
