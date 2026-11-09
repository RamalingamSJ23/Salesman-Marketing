namespace MarketingApi.DTOs.Common
{
    /// <summary>
    /// Represents an option for dropdowns (e.g., Status dropdown, Role dropdown).
    /// </summary>
    public class EnumOptionDto
    {
        public short Value { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayText { get; set; } = string.Empty;
    }
}
