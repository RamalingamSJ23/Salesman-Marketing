using System.ComponentModel.DataAnnotations;

namespace MarketingApi.DTOs.MarketingTeam
{
    /// <summary>
    /// Input DTO for updating an existing Marketing Team with row merge in one transaction.
    /// Note: Teamcode is read-only in Edit as per specification.
    /// Note: Status is NOT updated here — use /activate or /deactivate endpoints instead.
    /// </summary>
    public class UpdateMarketingTeamDto
    {
        [Required(ErrorMessage = "Team name is required.")]
        [MaxLength(100, ErrorMessage = "Team name cannot exceed 100 characters.")]
        public string Teamname { get; set; } = string.Empty;

        [Required(ErrorMessage = "Team leader is required.")]
        public long Teamleaderid { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [MinLength(2, ErrorMessage = "Description must be at least 2 characters.")]
        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Add at least one salesman to the team.")]
        public List<MarketingTeamDetailInputDto> Members { get; set; } = new List<MarketingTeamDetailInputDto>();
    }
}
