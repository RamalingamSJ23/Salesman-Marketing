using System;
using System.Collections.Generic;

namespace MarketingApi.Models;

public partial class User
{
    public long Id { get; set; }

    public string Username { get; set; } = null!;

    public string Passwordhash { get; set; } = null!;

    public string? Name { get; set; }

    public long Roleid { get; set; }

    public virtual Role Role { get; set; } = null!;
}
