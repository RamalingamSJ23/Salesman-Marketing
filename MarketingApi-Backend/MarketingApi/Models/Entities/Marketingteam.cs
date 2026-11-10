using System;
using System.Collections.Generic;

namespace MarketingApi.Models;

public partial class Marketingteam
{
    public long Id { get; set; }

    public string Teamcode { get; set; } = null!;

    public string Teamname { get; set; } = null!;

    public long Teamleaderid { get; set; }

    public string? Description { get; set; }

    public short Status { get; set; }

    public DateTime Createddate { get; set; }

    public DateTime? Modifieddate { get; set; }

    public virtual ICollection<Marketingteamdetail> Marketingteamdetails { get; set; } = new List<Marketingteamdetail>();

    public virtual Salesman Teamleader { get; set; } = null!;
}
