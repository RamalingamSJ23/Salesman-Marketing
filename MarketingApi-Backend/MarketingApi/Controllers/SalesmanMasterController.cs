using MarketingApi.DTOs.Salesman;
using MarketingApi.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketingApi.Controllers
{
    /// <summary>
    /// API Controller for Salesman Master.
    /// Provides endpoints to list, view, create, edit details, deactivate, and activate salesmen.
    /// Accessible via /api/SalesmanMaster.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class SalesmanMasterController : ControllerBase
    {
        private readonly ISalesmanService _salesmanService;

        public SalesmanMasterController(ISalesmanService salesmanService)
        {
            _salesmanService = salesmanService;
        }

        /// <summary>
        /// Gets a paginated list of salesmen with search and status filtering.
        /// GET /api/SalesmanMaster?search=john&amp;status=1&amp;pageNumber=1&amp;pageSize=10
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSalesmen([FromQuery] SalesmanQueryParameters parameters)
        {
            var result = await _salesmanService.GetPagedSalesmenAsync(parameters);
            return Ok(result.Data);
        }

        /// <summary>
        /// Gets a single salesman by ID.
        /// GET /api/SalesmanMaster/{id}
        /// </summary>
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _salesmanService.GetByIdAsync(id);
            if (!result.Success)
            {
                return StatusCode(result.StatusCode, new { message = result.ErrorMessage });
            }

            return Ok(result.Data);
        }

        /// <summary>
        /// Returns a list of all active salesmen for dropdown controls in Marketing Team forms.
        /// GET /api/SalesmanMaster/lookup
        /// </summary>
        [HttpGet("lookup")]
        public async Task<IActionResult> GetLookup()
        {
            var result = await _salesmanService.GetActiveSalesmenLookupAsync();
            return Ok(result.Data);
        }

        /// <summary>
        /// Creates a new salesman record.
        /// POST /api/SalesmanMaster
        /// Body: salesmancode, salesmanname, mobileno, email, joiningdate, status
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateSalesman([FromBody] CreateSalesmanDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _salesmanService.CreateSalesmanAsync(dto);
            if (!result.Success)
            {
                return StatusCode(result.StatusCode, new { message = result.ErrorMessage });
            }

            return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
        }

        /// <summary>
        /// Updates an existing salesman's details (Name, Mobile, Email, Joining Date only).
        /// Status is NOT changed here — use /deactivate or /activate endpoints.
        /// PUT /api/SalesmanMaster/{id}
        /// </summary>
        [HttpPut("{id:long}")]
        public async Task<IActionResult> UpdateSalesman(long id, [FromBody] UpdateSalesmanDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _salesmanService.UpdateSalesmanAsync(id, dto);
            if (!result.Success)
            {
                return StatusCode(result.StatusCode, new { message = result.ErrorMessage });
            }

            return Ok(result.Data);
        }

        /// <summary>
        /// Deactivates a salesman or sends a deactivation request to Marketing Master if assigned to an active team.
        /// PUT /api/SalesmanMaster/{id}/deactivate
        /// </summary>
        [HttpPut("{id:long}/deactivate")]
        public async Task<IActionResult> DeactivateSalesman(long id)
        {
            var user = User?.Identity?.Name ?? "superviser";
            var result = await _salesmanService.DeactivateSalesmanAsync(id, user);
            if (!result.Success)
            {
                return StatusCode(result.StatusCode, new { message = result.ErrorMessage });
            }

            return Ok(new
            {
                message = result.Data!.Message,
                directDeactivation = result.Data.DirectDeactivation,
                requestSentToMarketingMaster = result.Data.RequestSentToMarketingMaster,
                data = result.Data
            });
        }

        /// <summary>
        /// Checks if a salesman is currently assigned to an active marketing team.
        /// GET /api/SalesmanMaster/{id}/team-assignment
        /// </summary>
        [HttpGet("{id:long}/team-assignment")]
        public async Task<IActionResult> GetTeamAssignment(long id)
        {
            var result = await _salesmanService.CheckSalesmanTeamAssignmentAsync(id);
            return Ok(result.Data);
        }

        /// <summary>
        /// Gets all pending deactivation requests for Marketing Master.
        /// GET /api/SalesmanMaster/deactivation-requests/pending
        /// </summary>
        [HttpGet("deactivation-requests/pending")]
        public async Task<IActionResult> GetPendingDeactivationRequests()
        {
            var result = await _salesmanService.GetPendingDeactivationRequestsAsync();
            return Ok(result.Data);
        }

        /// <summary>
        /// Marketing Master approves a salesman deactivation request.
        /// If leader -> deactivates team, all team members, and salesman.
        /// If member -> deactivates member in team and salesman.
        /// POST /api/SalesmanMaster/deactivation-requests/{id}/approve
        /// </summary>
        [HttpPost("deactivation-requests/{id:long}/approve")]
        public async Task<IActionResult> ApproveDeactivation(long id)
        {
            var user = User?.Identity?.Name ?? "marketing";
            var result = await _salesmanService.ApproveDeactivationRequestAsync(id, user);
            if (!result.Success)
            {
                return StatusCode(result.StatusCode, new { message = result.ErrorMessage });
            }

            return Ok(new { message = "Deactivation request approved successfully.", data = result.Data });
        }

        /// <summary>
        /// Marketing Master rejects a salesman deactivation request.
        /// POST /api/SalesmanMaster/deactivation-requests/{id}/reject
        /// </summary>
        [HttpPost("deactivation-requests/{id:long}/reject")]
        public async Task<IActionResult> RejectDeactivation(long id, [FromBody] ProcessDeactivationRequestDto? dto)
        {
            var user = User?.Identity?.Name ?? "marketing";
            var result = await _salesmanService.RejectDeactivationRequestAsync(id, user, dto?.Notes);
            if (!result.Success)
            {
                return StatusCode(result.StatusCode, new { message = result.ErrorMessage });
            }

            return Ok(new { message = "Deactivation request rejected.", data = result.Data });
        }

        /// <summary>
        /// Activates a salesman (sets status = Active).
        /// PUT /api/SalesmanMaster/{id}/activate
        /// </summary>
        [HttpPut("{id:long}/activate")]
        public async Task<IActionResult> ActivateSalesman(long id)
        {
            var result = await _salesmanService.ActivateSalesmanAsync(id);
            if (!result.Success)
            {
                return StatusCode(result.StatusCode, new { message = result.ErrorMessage });
            }

            return Ok(new { message = "Salesman activated successfully.", data = result.Data });
        }
    }
}
