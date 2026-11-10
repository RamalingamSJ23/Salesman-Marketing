namespace MarketingApi.DTOs.Salesman
{
    /// <summary>
    /// Query string parameters for filtering and paging salesmen.
    /// </summary>
    public class SalesmanQueryParameters
    {
        public string? Search { get; set; }
        public short? Status { get; set; } // null = All, 1 = Active, 2 = Inactive
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
