using System.ComponentModel.DataAnnotations;

namespace MarketingApi.DTOs.Salesman;

/// <summary>
/// Query string parameters for filtering, sorting, and paging salesmen.
/// </summary>
public class SalesmanQueryParameters
{
    public string? Search { get; set; }
    public short? Status { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "PageNumber must be at least 1.")]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100.")]
    public int PageSize { get; set; } = 10;

    public string? SortBy { get; set; } = "salesmancode";
    public string? SortDir { get; set; } = "asc";
}
