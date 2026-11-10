namespace MarketingApi.Models.Entities;

/// <summary>
/// Marks entities whose creation and modification timestamps and users are automatically populated on save.
/// </summary>
public interface IAuditableEntity
{
    DateTime CreatedDate { get; set; }
    string? CreatedBy { get; set; }
    DateTime? ModifiedDate { get; set; }
    string? ModifiedBy { get; set; }
}
