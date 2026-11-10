namespace MarketingApi.Models.Entities;

public partial class User
{
    public long Id { get; set; }
    public string Username { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string? Name { get; set; }
    public long RoleId { get; set; }

    public virtual Role Role { get; set; } = null!;
}
