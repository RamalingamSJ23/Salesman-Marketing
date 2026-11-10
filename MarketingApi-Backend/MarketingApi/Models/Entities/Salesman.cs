using System;
using System.Collections.Generic;

namespace MarketingApi.Models;

public partial class Salesman
{
    public long Id { get; set; }

    public string Salesmancode { get; set; } = null!;

    public string Salesmanname { get; set; } = null!;

    public string? Mobileno { get; set; }

    public string? Email { get; set; }

    public DateOnly? Joiningdate { get; set; }

    public short Status { get; set; }

    public DateTime Createddate { get; set; }

    public DateTime? Modifieddate { get; set; }

    public virtual ICollection<Marketingteamdetail> Marketingteamdetails { get; set; } = new List<Marketingteamdetail>();

    public virtual ICollection<Marketingteam> Marketingteams { get; set; } = new List<Marketingteam>();
}
