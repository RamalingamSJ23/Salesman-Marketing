using System.ComponentModel.DataAnnotations;

namespace MarketingApi.DTOs.MarketingTeam;

/// <summary>
/// Query string parameters for filtering, sorting, and paging marketing teams.
/// </summary>
public class MarketingTeamQueryParameters
{
    public string? Search { get; set; }
    public short? Status { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "PageNumber must be at least 1.")]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100.")]
    public int PageSize { get; set; } = 10;

    public string? SortBy { get; set; } = "teamcode";
    public string? SortDir { get; set; } = "asc";
}
