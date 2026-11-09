using System.ComponentModel.DataAnnotations;

namespace MarketingApi.DTOs.MarketingTeam
{
    /// <summary>
    /// Input DTO for creating a new Marketing Team with its members in one transaction.
    /// </summary>
    public class CreateMarketingTeamDto
    {
        [Required(ErrorMessage = "Team code is required.")]
        [MaxLength(20, ErrorMessage = "Team code cannot exceed 20 characters.")]
        public string Teamcode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Team name is required.")]
        [MaxLength(100, ErrorMessage = "Team name cannot exceed 100 characters.")]
        public string Teamname { get; set; } = string.Empty;

        [Required(ErrorMessage = "Team leader is required.")]
        public long Teamleaderid { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [MinLength(2, ErrorMessage = "Description must be at least 2 characters.")]
        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string Description { get; set; } = string.Empty;

        [Range(1, 2, ErrorMessage = "Status must be 1 (Active) or 2 (Inactive).")]
        public short Status { get; set; } = 1;

        [Required(ErrorMessage = "Add at least one salesman to the team.")]
        public List<MarketingTeamDetailInputDto> Members { get; set; } = new List<MarketingTeamDetailInputDto>();
    }
}
