namespace MarketingApi.Models.Entities;

public partial class MarketingTeam : IAuditableEntity
{
    public long Id { get; set; }
    public string TeamCode { get; set; } = null!;
    public string TeamName { get; set; } = null!;
    public long TeamLeaderId { get; set; }
    public string? Description { get; set; }
    public short Status { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }

    public virtual ICollection<MarketingTeamDetail> MarketingTeamDetails { get; set; } = new List<MarketingTeamDetail>();
    public virtual Salesman TeamLeader { get; set; } = null!;
}
