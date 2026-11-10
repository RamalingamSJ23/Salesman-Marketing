using MarketingApi.Common;
using MarketingApi.DTOs.MarketingTeam;
using MarketingApi.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketingApi.Controllers;

/// <summary>
/// API Controller for Marketing Team Master.
/// Accessible via /api/MarketingTeamMaster.
/// </summary>
[ApiController]
[Authorize(Policy = AppRoles.MarketingPolicy)]
[Route("api/[controller]")]
public class MarketingTeamMasterController : ControllerBase
{
    private readonly IMarketingTeamService _marketingTeamService;

    public MarketingTeamMasterController(IMarketingTeamService marketingTeamService)
    {
        _marketingTeamService = marketingTeamService;
    }

    [HttpGet]
    public async Task<IActionResult> GetTeams([FromQuery] MarketingTeamQueryParameters parameters)
    {
        var result = await _marketingTeamService.GetPagedTeamsAsync(parameters);
        return result.ToActionResult(this);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _marketingTeamService.GetByIdAsync(id);
        return result.ToActionResult(this);
    }

    [HttpGet("role-options")]
    public IActionResult GetRoleOptions()
    {
        var options = _marketingTeamService.GetRoleOptions();
        return Ok(options);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTeam([FromBody] CreateMarketingTeamDto dto)
    {
        var result = await _marketingTeamService.CreateTeamAsync(dto);
        return result.ToActionResult(this, nameof(GetById), data => new { id = data.Id });
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateTeam(long id, [FromBody] UpdateMarketingTeamDto dto)
    {
        var result = await _marketingTeamService.UpdateTeamAsync(id, dto);
        return result.ToActionResult(this);
    }

    [HttpPut("{id:long}/deactivate")]
    public async Task<IActionResult> DeactivateTeam(long id)
    {
        var result = await _marketingTeamService.DeactivateTeamAsync(id);
        return result.ToActionResult(this);
    }

    [HttpPut("{id:long}/activate")]
    public async Task<IActionResult> ActivateTeam(long id)
    {
        var result = await _marketingTeamService.ActivateTeamAsync(id);
        return result.ToActionResult(this);
    }

    [HttpPost("{id:long}/salesmen")]
    public async Task<IActionResult> AddSalesman(long id, [FromBody] MarketingTeamDetailInputDto dto)
    {
        var result = await _marketingTeamService.AddSalesmanToTeamAsync(id, dto);
        return result.ToActionResult(this);
    }
}
