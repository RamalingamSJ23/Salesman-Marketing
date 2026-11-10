namespace MarketingApi.DTOs.MarketingTeam
{
    /// <summary>
    /// Represents a row in the Marketing Team Master list.
    /// Matched with specification join query:
    /// id, teamcode, teamname, teamleadername, salesmancount, status
    /// </summary>
    public class MarketingTeamListDto
    {
        public long Id { get; set; }
        public string Teamcode { get; set; } = string.Empty;
        public string Teamname { get; set; } = string.Empty;
        public string Teamleadername { get; set; } = string.Empty;
        public int Salesmancount { get; set; }
        public short Status { get; set; }
        public string StatusName => Status == 1 ? "Active" : "Inactive";
    }
}
