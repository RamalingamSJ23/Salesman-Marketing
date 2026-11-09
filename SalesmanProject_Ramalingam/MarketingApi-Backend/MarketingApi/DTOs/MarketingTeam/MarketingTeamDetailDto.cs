namespace MarketingApi.DTOs.MarketingTeam
{
    /// <summary>
    /// Represents a member row when viewing or loading a marketing team.
    /// </summary>
    public class MarketingTeamDetailDto
    {
        public long Id { get; set; }
        public long Salesmanid { get; set; }
        public string Salesmancode { get; set; } = string.Empty;
        public string Salesmanname { get; set; } = string.Empty;
        public short Role { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public decimal Targetamount { get; set; }
        public short Status { get; set; }
        public string StatusName => Status == 1 ? "Active" : "Inactive";
    }
}
