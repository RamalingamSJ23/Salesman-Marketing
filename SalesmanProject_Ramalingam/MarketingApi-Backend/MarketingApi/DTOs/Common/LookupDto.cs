namespace MarketingApi.DTOs.Common
{
    /// <summary>
    /// Simple DTO for dropdown selections (e.g. Salesman dropdown in Team form).
    /// Includes active team assignments so the UI can filter eligible leaders and members.
    /// </summary>
    public class SalesmanLookupDto
    {
        public long Id { get; set; }
        public string Salesmancode { get; set; } = string.Empty;
        public string Salesmanname { get; set; } = string.Empty;
        public short Status { get; set; }
        public long? ActiveTeamId { get; set; }
        public string? ActiveTeamCode { get; set; }
        public bool IsLeaderInActiveTeam { get; set; }
    }
}
