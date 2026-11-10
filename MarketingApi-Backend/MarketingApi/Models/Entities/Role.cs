using System.Text.Json.Serialization;

namespace MarketingApi.Models.Entities;

public partial class Role
{
    public long Id { get; set; }
    public string RoleName { get; set; } = null!;

    [JsonIgnore]
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
