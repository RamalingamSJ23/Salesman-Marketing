using MarketingApi.Common;
using MarketingApi.DTOs.Salesman;
using MarketingApi.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketingApi.Controllers;

/// <summary>
/// API Controller for Salesman Master and deactivation approval requests.
/// Accessible via /api/SalesmanMaster.
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class SalesmanMasterController : ControllerBase
{
    private readonly ISalesmanService _salesmanService;
    private readonly ICurrentUserService _currentUser;

    public SalesmanMasterController(ISalesmanService salesmanService, ICurrentUserService currentUser)
    {
        _salesmanService = salesmanService;
        _currentUser = currentUser;
    }

    [HttpGet]
    [Authorize(Policy = AppRoles.SalesmanPolicy)]
    public async Task<IActionResult> GetSalesmen([FromQuery] SalesmanQueryParameters parameters)
    {
        var result = await _salesmanService.GetPagedSalesmenAsync(parameters);
        return result.ToActionResult(this);
    }

    [HttpGet("{id:long}")]
    [Authorize(Policy = AppRoles.SalesmanPolicy)]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _salesmanService.GetByIdAsync(id);
        return result.ToActionResult(this);
    }

    [HttpGet("lookup")]
    [Authorize(Policy = AppRoles.AnyMasterPolicy)]
    public async Task<IActionResult> GetLookup()
    {
        var result = await _salesmanService.GetActiveSalesmenLookupAsync();
        return result.ToActionResult(this);
    }

    [HttpPost]
    [Authorize(Policy = AppRoles.SalesmanPolicy)]
    public async Task<IActionResult> CreateSalesman([FromBody] CreateSalesmanDto dto)
    {
        var result = await _salesmanService.CreateSalesmanAsync(dto);
        return result.ToActionResult(this, nameof(GetById), data => new { id = data.Id });
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = AppRoles.SalesmanPolicy)]
    public async Task<IActionResult> UpdateSalesman(long id, [FromBody] UpdateSalesmanDto dto)
    {
        var result = await _salesmanService.UpdateSalesmanAsync(id, dto);
        return result.ToActionResult(this);
    }

    [HttpPut("{id:long}/deactivate")]
    [Authorize(Policy = AppRoles.SalesmanPolicy)]
    public async Task<IActionResult> DeactivateSalesman(long id)
    {
        var result = await _salesmanService.DeactivateSalesmanAsync(id, _currentUser.GetRequiredUsername());
        return result.ToActionResult(this);
    }

    [HttpGet("{id:long}/team-assignment")]
    [Authorize(Policy = AppRoles.SalesmanPolicy)]
    public async Task<IActionResult> GetTeamAssignment(long id)
    {
        var result = await _salesmanService.CheckSalesmanTeamAssignmentAsync(id);
        return result.ToActionResult(this);
    }

    [HttpGet("deactivation-requests/pending")]
    [Authorize(Policy = AppRoles.MarketingPolicy)]
    public async Task<IActionResult> GetPendingDeactivationRequests()
    {
        var result = await _salesmanService.GetPendingDeactivationRequestsAsync();
        return result.ToActionResult(this);
    }

    [HttpPost("deactivation-requests/{id:long}/approve")]
    [Authorize(Policy = AppRoles.MarketingPolicy)]
    public async Task<IActionResult> ApproveDeactivation(long id)
    {
        var result = await _salesmanService.ApproveDeactivationRequestAsync(id, _currentUser.GetRequiredUsername());
        return result.ToActionResult(this);
    }

    [HttpPost("deactivation-requests/{id:long}/reject")]
    [Authorize(Policy = AppRoles.MarketingPolicy)]
    public async Task<IActionResult> RejectDeactivation(long id, [FromBody] ProcessDeactivationRequestDto? dto)
    {
        var result = await _salesmanService.RejectDeactivationRequestAsync(id, _currentUser.GetRequiredUsername(), dto?.Notes);
        return result.ToActionResult(this);
    }

    [HttpPut("{id:long}/activate")]
    [Authorize(Policy = AppRoles.SalesmanPolicy)]
    public async Task<IActionResult> ActivateSalesman(long id)
    {
        var result = await _salesmanService.ActivateSalesmanAsync(id, _currentUser.GetRequiredUsername());
        return result.ToActionResult(this);
    }
}
