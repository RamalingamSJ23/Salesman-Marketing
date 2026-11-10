using MarketingApi.Enums;

namespace MarketingApi.Models.Entities;

public partial class SalesmanDeactivationRequest
{
    public long Id { get; set; }
    public long SalesmanId { get; set; }
    public string SalesmanCode { get; set; } = null!;
    public string SalesmanName { get; set; } = null!;
    public long? TeamId { get; set; }
    public string? TeamCode { get; set; }
    public string? TeamName { get; set; }
    public string RoleInTeam { get; set; } = null!;
    public string? Reason { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
    public string Status { get; set; } = nameof(DeactivationRequestStatus.Pending);
    public DateTime RequestedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedDate { get; set; }
    public string? ProcessedBy { get; set; }
    public string? Notes { get; set; }
    public uint Version { get; set; }

    public virtual Salesman Salesman { get; set; } = null!;
    public virtual MarketingTeam? Team { get; set; }
}
