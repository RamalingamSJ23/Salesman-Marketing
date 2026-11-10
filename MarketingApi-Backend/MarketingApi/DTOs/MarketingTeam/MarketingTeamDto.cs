namespace MarketingApi.DTOs.MarketingTeam
{
    /// <summary>
    /// Full Marketing Team details for View / Edit screen (Header + Members + Total Target).
    /// </summary>
    public class MarketingTeamDto
    {
        public long Id { get; set; }
        public string Teamcode { get; set; } = string.Empty;
        public string Teamname { get; set; } = string.Empty;
        public long Teamleaderid { get; set; }
        public string Teamleadername { get; set; } = string.Empty;
        public string? Description { get; set; }
        public short Status { get; set; }
        public string StatusName => Status == 1 ? "Active" : "Inactive";
        public DateTime Createddate { get; set; }
        public DateTime? Modifieddate { get; set; }

        public decimal TotalTargetAmount => Members.Sum(m => m.Targetamount);
        public List<MarketingTeamDetailDto> Members { get; set; } = new List<MarketingTeamDetailDto>();
    }
}
