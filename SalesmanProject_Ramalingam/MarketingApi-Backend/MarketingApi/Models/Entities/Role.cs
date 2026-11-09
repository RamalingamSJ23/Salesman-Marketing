using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MarketingApi.Models;

public partial class Role
{
    public long Id { get; set; }

    public string Rolename { get; set; } = null!;

    [JsonIgnore]
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
