using MarketingApi.Common;
using MarketingApi.DTOs.Common;
using MarketingApi.DTOs.Salesman;
using MarketingApi.Enums;
using MarketingApi.Models;
using MarketingApi.Repository;
using System.Text.RegularExpressions;

namespace MarketingApi.Service
{
    /// <summary>
    /// Implements business logic and validation rules for Salesman Master.
    /// </summary>
    public class SalesmanService : ISalesmanService
    {
        private readonly ISalesmanRepository _salesmanRepository;

        public SalesmanService(ISalesmanRepository salesmanRepository)
        {
            _salesmanRepository = salesmanRepository;
        }

        /// <summary>
        /// Retrieves a paginated list of salesmen with optional search and status filter.
        /// </summary>
        public async Task<ServiceResult<PagedResultDto<SalesmanListDto>>> GetPagedSalesmenAsync(SalesmanQueryParameters parameters)
        {
            // Set safe defaults for pagination
            if (parameters.PageNumber <= 0) parameters.PageNumber = 1;
            if (parameters.PageSize <= 0) parameters.PageSize = 10;

            var result = await _salesmanRepository.GetPagedSalesmenAsync(parameters);
            return ServiceResult<PagedResultDto<SalesmanListDto>>.Ok(result);
        }

        /// <summary>
        /// Gets a single salesman by ID.
        /// </summary>
        public async Task<ServiceResult<SalesmanDto>> GetByIdAsync(long id)
        {
            var salesman = await _salesmanRepository.GetByIdAsync(id);
            if (salesman == null)
            {
                return ServiceResult<SalesmanDto>.NotFound($"Salesman with ID {id} was not found.");
            }

            return ServiceResult<SalesmanDto>.Ok(MapToDto(salesman));
        }

        /// <summary>
        /// Creates a new salesman.
        /// Enforces Rule R1 (Unique code) and date/mobile validations.
        /// </summary>
        public async Task<ServiceResult<SalesmanDto>> CreateSalesmanAsync(CreateSalesmanDto dto)
        {
            var cleanCode = dto.Salesmancode.Trim().ToUpper();

            // Rule R1: Salesman code is required and unique
            var codeExists = await _salesmanRepository.CodeExistsAsync(cleanCode);
            if (codeExists)
            {
                return ServiceResult<SalesmanDto>.Conflict($"Salesman code {cleanCode} already exists.");
            }

            // Validation: Salesman name (English letters, spaces, standard punctuation only)
            if (!Regex.IsMatch(dto.Salesmanname.Trim(), @"^[a-zA-Z\s.'-]+$"))
            {
                return ServiceResult<SalesmanDto>.BadRequest("Salesman name must contain English letters only.");
            }

            // Validation: Joining date cannot be in the future
            if (dto.Joiningdate.HasValue && dto.Joiningdate.Value > DateOnly.FromDateTime(DateTime.Today))
            {
                return ServiceResult<SalesmanDto>.BadRequest("Joining date cannot be in the future.");
            }

            // Validation: Mobile number format (digits, 7-15 chars) and uniqueness
            if (!string.IsNullOrWhiteSpace(dto.Mobileno))
            {
                var cleanMobile = dto.Mobileno.Trim();
                if (!Regex.IsMatch(cleanMobile, @"^[0-9]{7,15}$"))
                {
                    return ServiceResult<SalesmanDto>.BadRequest("Mobile number must be between 7 and 15 digits.");
                }

                var mobileExists = await _salesmanRepository.MobileExistsAsync(cleanMobile);
                if (mobileExists)
                {
                    return ServiceResult<SalesmanDto>.Conflict($"Mobile number {cleanMobile} already exists.");
                }
            }

            // Validation: Email format, uniqueness, and lowercase only (no capital letters)
            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                var cleanEmail = dto.Email.Trim();
                if (cleanEmail.Any(char.IsUpper))
                {
                    return ServiceResult<SalesmanDto>.BadRequest("Capital letters are not allowed in email. Email must be in lowercase only.");
                }

                var emailExists = await _salesmanRepository.EmailExistsAsync(cleanEmail);
                if (emailExists)
                {
                    return ServiceResult<SalesmanDto>.Conflict($"Email address {cleanEmail} already exists.");
                }
            }

            // Create entity
            var salesman = new Salesman
            {
                Salesmancode = cleanCode,
                Salesmanname = dto.Salesmanname.Trim(),
                Mobileno = string.IsNullOrWhiteSpace(dto.Mobileno) ? null : dto.Mobileno.Trim(),
                Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
                Joiningdate = dto.Joiningdate,
                Status = dto.Status == 2 ? (short)RecordStatus.Inactive : (short)RecordStatus.Active,
                Createddate = DateTime.UtcNow
            };

            var created = await _salesmanRepository.CreateAsync(salesman);
            return ServiceResult<SalesmanDto>.Created(MapToDto(created));
        }

        /// <summary>
        /// Updates an existing salesman details (Name, Mobile, Email, Joining Date).
        /// Note: Salesmancode and Status are NOT modified here.
        /// Status is changed via dedicated Deactivate/Activate endpoints.
        /// </summary>
        public async Task<ServiceResult<SalesmanDto>> UpdateSalesmanAsync(long id, UpdateSalesmanDto dto)
        {
            var salesman = await _salesmanRepository.GetByIdAsync(id);
            if (salesman == null)
            {
                return ServiceResult<SalesmanDto>.NotFound($"Salesman with ID {id} was not found.");
            }

            // Validation: Joining date cannot be in the future
            if (dto.Joiningdate.HasValue && dto.Joiningdate.Value > DateOnly.FromDateTime(DateTime.Today))
            {
                return ServiceResult<SalesmanDto>.BadRequest("Joining date cannot be in the future.");
            }

            // Validation: Mobile number format and uniqueness
            if (!string.IsNullOrWhiteSpace(dto.Mobileno))
            {
                var cleanMobile = dto.Mobileno.Trim();
                if (!Regex.IsMatch(cleanMobile, @"^[0-9]{7,15}$"))
                {
                    return ServiceResult<SalesmanDto>.BadRequest("Mobile number must be between 7 and 15 digits.");
                }

                var mobileExists = await _salesmanRepository.MobileExistsAsync(cleanMobile, id);
                if (mobileExists)
                {
                    return ServiceResult<SalesmanDto>.Conflict($"Mobile number {cleanMobile} already exists.");
                }
            }

            // Validation: Email format, uniqueness, and lowercase only (no capital letters)
            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                var cleanEmail = dto.Email.Trim();
                if (cleanEmail.Any(char.IsUpper))
                {
                    return ServiceResult<SalesmanDto>.BadRequest("Capital letters are not allowed in email. Email must be in lowercase only.");
                }

                var emailExists = await _salesmanRepository.EmailExistsAsync(cleanEmail, id);
                if (emailExists)
                {
                    return ServiceResult<SalesmanDto>.Conflict($"Email address {cleanEmail} already exists.");
                }
            }

            // Validation: Salesman name (English letters, spaces, standard punctuation only)
            if (!Regex.IsMatch(dto.Salesmanname.Trim(), @"^[a-zA-Z\s.'-]+$"))
            {
                return ServiceResult<SalesmanDto>.BadRequest("Salesman name must contain English letters only.");
            }

            // Update fields (Status is preserved as-is!)
            salesman.Salesmanname = dto.Salesmanname.Trim();
            salesman.Mobileno = string.IsNullOrWhiteSpace(dto.Mobileno) ? null : dto.Mobileno.Trim();
            salesman.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
            salesman.Joiningdate = dto.Joiningdate;
            salesman.Modifieddate = DateTime.UtcNow;

            await _salesmanRepository.UpdateAsync(salesman);
            return ServiceResult<SalesmanDto>.Ok(MapToDto(salesman));
        }

        /// <summary>
        /// Checks if a salesman is currently assigned to an active marketing team (as leader or member).
        /// </summary>
        public async Task<ServiceResult<ActiveTeamAssignmentDto?>> CheckSalesmanTeamAssignmentAsync(long salesmanId)
        {
            var assignment = await _salesmanRepository.GetActiveTeamAssignmentAsync(salesmanId);
            return ServiceResult<ActiveTeamAssignmentDto?>.Ok(assignment);
        }

        /// <summary>
        /// Deactivates a salesman by ID.
        /// If the salesman is an active Team Leader or Team Member, sends a deactivation request to Marketing Master.
        /// If the salesman has no active team assignment, deactivates directly.
        /// </summary>
        public async Task<ServiceResult<DeactivateSalesmanResultDto>> DeactivateSalesmanAsync(long id, string requestedBy = "superviser")
        {
            var salesman = await _salesmanRepository.GetByIdAsync(id);
            if (salesman == null)
            {
                return ServiceResult<DeactivateSalesmanResultDto>.NotFound($"Salesman with ID {id} was not found.");
            }

            // Check if there is already a pending deactivation request
            var existingPending = await _salesmanRepository.GetPendingRequestForSalesmanAsync(id);
            if (existingPending != null)
            {
                return ServiceResult<DeactivateSalesmanResultDto>.BadRequest(
                    $"A deactivation request for salesman {salesman.Salesmancode} is already pending approval from Marketing Master.");
            }

            // Check if salesman is assigned to any active team (as Leader or Member)
            var assignment = await _salesmanRepository.GetActiveTeamAssignmentAsync(id);
            if (assignment != null)
            {
                // Must send deactivation request to Marketing Master!
                var request = new SalesmanDeactivationRequest
                {
                    Salesmanid = salesman.Id,
                    Salesmancode = salesman.Salesmancode,
                    Salesmanname = salesman.Salesmanname,
                    Teamid = assignment.TeamId,
                    Teamcode = assignment.TeamCode,
                    Teamname = assignment.TeamName,
                    Roleinteam = assignment.RoleInTeam,
                    Reason = $"Salesman deactivation requested while assigned as {assignment.RoleInTeam} in Team {assignment.TeamCode}.",
                    Requestedby = requestedBy,
                    Status = "Pending",
                    Requesteddate = DateTime.UtcNow
                };

                var createdRequest = await _salesmanRepository.CreateDeactivationRequestAsync(request);

                return ServiceResult<DeactivateSalesmanResultDto>.Ok(new DeactivateSalesmanResultDto
                {
                    DirectDeactivation = false,
                    RequestSentToMarketingMaster = true,
                    Message = $"Deactivation request submitted to Marketing Master for approval. Salesman {salesman.Salesmancode} is currently {assignment.RoleInTeam} in Team {assignment.TeamCode}.",
                    Salesman = MapToDto(salesman),
                    DeactivationRequest = MapToRequestDto(createdRequest)
                });
            }

            // Not assigned to any active team -> direct deactivation!
            salesman.Status = (short)RecordStatus.Inactive; // 2
            salesman.Modifieddate = DateTime.UtcNow;

            await _salesmanRepository.UpdateAsync(salesman);

            return ServiceResult<DeactivateSalesmanResultDto>.Ok(new DeactivateSalesmanResultDto
            {
                DirectDeactivation = true,
                RequestSentToMarketingMaster = false,
                Message = $"Salesman {salesman.Salesmancode} deactivated successfully.",
                Salesman = MapToDto(salesman)
            });
        }

        public async Task<ServiceResult<List<SalesmanDeactivationRequestDto>>> GetPendingDeactivationRequestsAsync()
        {
            var list = await _salesmanRepository.GetPendingDeactivationRequestsAsync();
            return ServiceResult<List<SalesmanDeactivationRequestDto>>.Ok(list.Select(MapToRequestDto).ToList());
        }

        public async Task<ServiceResult<SalesmanDeactivationRequestDto>> ApproveDeactivationRequestAsync(long requestId, string processedBy = "marketing")
        {
            var request = await _salesmanRepository.GetDeactivationRequestByIdAsync(requestId);
            if (request == null)
            {
                return ServiceResult<SalesmanDeactivationRequestDto>.NotFound($"Deactivation request with ID {requestId} was not found.");
            }

            if (request.Status != "Pending")
            {
                return ServiceResult<SalesmanDeactivationRequestDto>.BadRequest($"Request is already {request.Status}.");
            }

            var salesman = await _salesmanRepository.GetByIdAsync(request.Salesmanid);
            if (salesman == null)
            {
                return ServiceResult<SalesmanDeactivationRequestDto>.NotFound($"Salesman with ID {request.Salesmanid} was not found.");
            }

            // If salesman was Team Leader of the team:
            if (request.Roleinteam == "Leader" && request.Teamid.HasValue)
            {
                // Deactivate team and all members inside the team
                await _salesmanRepository.DeactivateTeamAndMembersAsync(request.Teamid.Value);
            }
            else if (request.Teamid.HasValue)
            {
                // If salesman was team member: deactivate member inside team
                await _salesmanRepository.DeactivateTeamMemberAsync(request.Teamid.Value, request.Salesmanid);
            }

            // Deactivate salesman in salesman master
            salesman.Status = (short)RecordStatus.Inactive; // 2
            salesman.Modifieddate = DateTime.UtcNow;
            await _salesmanRepository.UpdateAsync(salesman);

            // Mark request approved
            request.Status = "Approved";
            request.Processeddate = DateTime.UtcNow;
            request.Processedby = processedBy;
            await _salesmanRepository.UpdateDeactivationRequestAsync(request);

            return ServiceResult<SalesmanDeactivationRequestDto>.Ok(MapToRequestDto(request));
        }

        public async Task<ServiceResult<SalesmanDeactivationRequestDto>> RejectDeactivationRequestAsync(long requestId, string processedBy = "marketing", string? notes = null)
        {
            var request = await _salesmanRepository.GetDeactivationRequestByIdAsync(requestId);
            if (request == null)
            {
                return ServiceResult<SalesmanDeactivationRequestDto>.NotFound($"Deactivation request with ID {requestId} was not found.");
            }

            if (request.Status != "Pending")
            {
                return ServiceResult<SalesmanDeactivationRequestDto>.BadRequest($"Request is already {request.Status}.");
            }

            request.Status = "Rejected";
            request.Processeddate = DateTime.UtcNow;
            request.Processedby = processedBy;
            request.Notes = notes;
            await _salesmanRepository.UpdateDeactivationRequestAsync(request);

            return ServiceResult<SalesmanDeactivationRequestDto>.Ok(MapToRequestDto(request));
        }

        /// <summary>
        /// Activates a salesman by ID (sets status = 1, Active).
        /// </summary>
        public async Task<ServiceResult<SalesmanDto>> ActivateSalesmanAsync(long id)
        {
            var salesman = await _salesmanRepository.GetByIdAsync(id);
            if (salesman == null)
            {
                return ServiceResult<SalesmanDto>.NotFound($"Salesman with ID {id} was not found.");
            }

            salesman.Status = (short)RecordStatus.Active; // 1
            salesman.Modifieddate = DateTime.UtcNow;

            await _salesmanRepository.UpdateAsync(salesman);
            return ServiceResult<SalesmanDto>.Ok(MapToDto(salesman));
        }

        /// <summary>
        /// Deletes a salesman.
        /// Enforces Rule R2: If assigned to a team, cannot be deleted.
        /// </summary>
        public async Task<ServiceResult> DeleteSalesmanAsync(long id)
        {
            var salesman = await _salesmanRepository.GetByIdAsync(id);
            if (salesman == null)
            {
                return ServiceResult.NotFound($"Salesman with ID {id} was not found.");
            }

            // Rule R2: A salesman assigned to any team cannot be deleted
            var isAssigned = await _salesmanRepository.IsAssignedToAnyTeamAsync(id);
            if (isAssigned)
            {
                return ServiceResult.BadRequest("Salesman is assigned to a team. Make them Inactive instead.");
            }

            await _salesmanRepository.DeleteAsync(salesman);
            return ServiceResult.Ok();
        }

        /// <summary>
        /// Returns active salesmen for dropdown controls in Marketing Team forms.
        /// </summary>
        public async Task<ServiceResult<List<SalesmanLookupDto>>> GetActiveSalesmenLookupAsync()
        {
            var lookup = await _salesmanRepository.GetActiveSalesmenLookupAsync();
            return ServiceResult<List<SalesmanLookupDto>>.Ok(lookup);
        }

        /// <summary>
        /// Helper method to map a Salesman entity to SalesmanDto.
        /// </summary>
        private static SalesmanDto MapToDto(Salesman s)
        {
            return new SalesmanDto
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
            };
        }

        private static SalesmanDeactivationRequestDto MapToRequestDto(SalesmanDeactivationRequest r)
        {
            return new SalesmanDeactivationRequestDto
            {
                Id = r.Id,
                SalesmanId = r.Salesmanid,
                SalesmanCode = r.Salesmancode,
                SalesmanName = r.Salesmanname,
                TeamId = r.Teamid,
                TeamCode = r.Teamcode,
                TeamName = r.Teamname,
                RoleInTeam = r.Roleinteam,
                Reason = r.Reason,
                RequestedBy = r.Requestedby,
                Status = r.Status,
                RequestedDate = r.Requesteddate,
                ProcessedDate = r.Processeddate,
                ProcessedBy = r.Processedby,
                Notes = r.Notes
            };
        }
    }
}
