using System;

namespace MarketingApi.Models;

public partial class SalesmanDeactivationRequest
{
    public long Id { get; set; }

    public long Salesmanid { get; set; }

    public string Salesmancode { get; set; } = null!;

    public string Salesmanname { get; set; } = null!;

    public long? Teamid { get; set; }

    public string? Teamcode { get; set; }

    public string? Teamname { get; set; }

    public string Roleinteam { get; set; } = null!; // "Leader" or "Member"

    public string? Reason { get; set; }

    public string Requestedby { get; set; } = "superviser";

    public string Status { get; set; } = "Pending"; // "Pending", "Approved", "Rejected"

    public DateTime Requesteddate { get; set; } = DateTime.UtcNow;

    public DateTime? Processeddate { get; set; }

    public string? Processedby { get; set; }

    public string? Notes { get; set; }

    public virtual Salesman Salesman { get; set; } = null!;

    public virtual Marketingteam? Team { get; set; }
}
