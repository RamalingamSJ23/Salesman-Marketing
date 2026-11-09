namespace MarketingApi.DTOs.Salesman
{
    /// <summary>
    /// Represents a row in the Salesman Master grid.
    /// </summary>
    public class SalesmanListDto
    {
        public long Id { get; set; }
        public string Salesmancode { get; set; } = string.Empty;
        public string Salesmanname { get; set; } = string.Empty;
        public string? Mobileno { get; set; }
        public string? Email { get; set; }
        public DateOnly? Joiningdate { get; set; }
        public short Status { get; set; }
        public string StatusName => Status == 1 ? "Active" : "Inactive";
        public DateTime Createddate { get; set; }
        public DateTime? Modifieddate { get; set; }
        public bool HasPendingDeactivation { get; set; }
        public string? PendingDeactivationTeam { get; set; }
        public string? PendingDeactivationRole { get; set; }
    }
}
