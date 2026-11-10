namespace MarketingApi.Models.Entities;

public partial class MarketingTeamDetail
{
    public long Id { get; set; }
    public long MarketingTeamId { get; set; }
    public long SalesmanId { get; set; }
    public short Role { get; set; }
    public decimal TargetAmount { get; set; }
    public short Status { get; set; }

    public virtual MarketingTeam MarketingTeam { get; set; } = null!;
    public virtual Salesman Salesman { get; set; } = null!;
}
