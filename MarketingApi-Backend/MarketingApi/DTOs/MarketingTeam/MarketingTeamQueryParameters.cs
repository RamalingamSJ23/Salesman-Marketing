namespace MarketingApi.DTOs.MarketingTeam
{
    /// <summary>
    /// Query string parameters for filtering and paging Marketing Teams.
    /// </summary>
    public class MarketingTeamQueryParameters
    {
        public string? Search { get; set; }
        public short? Status { get; set; } // null = All, 1 = Active, 2 = Inactive
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
