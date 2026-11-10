using System;
using System.Collections.Generic;

namespace MarketingApi.Models;

public partial class Marketingteamdetail
{
    public long Id { get; set; }

    public long Marketingteamid { get; set; }

    public long Salesmanid { get; set; }

    public short Role { get; set; }

    public decimal Targetamount { get; set; }

    public short Status { get; set; }

    public virtual Marketingteam Marketingteam { get; set; } = null!;

    public virtual Salesman Salesman { get; set; } = null!;
}
