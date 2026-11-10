namespace MarketingApi.Models.Entities;

public partial class Salesman : IAuditableEntity
{
    public long Id { get; set; }
    public string SalesmanCode { get; set; } = null!;
    public string SalesmanName { get; set; } = null!;
    public string? MobileNo { get; set; }
    public string? Email { get; set; }
    public DateOnly? JoiningDate { get; set; }
    public short Status { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }

    public virtual ICollection<MarketingTeamDetail> MarketingTeamDetails { get; set; } = new List<MarketingTeamDetail>();
    public virtual ICollection<MarketingTeam> MarketingTeams { get; set; } = new List<MarketingTeam>();
}
