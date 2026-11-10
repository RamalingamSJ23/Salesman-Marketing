using System.ComponentModel.DataAnnotations;

namespace MarketingApi.DTOs.MarketingTeam
{
    /// <summary>
    /// Input DTO for a team member row in create/update operations.
    /// </summary>
    public class MarketingTeamDetailInputDto
    {
        /// <summary>
        /// Detail row ID. Null or 0 for newly added rows; existing ID for edited rows.
        /// </summary>
        public long? Id { get; set; }

        [Required(ErrorMessage = "Salesman is required.")]
        public long Salesmanid { get; set; }

        [Required(ErrorMessage = "Role is required.")]
        [Range(1, 4, ErrorMessage = "Role must be 1 (TeamLeader), 2 (SeniorSalesExecutive), 3 (SalesExecutive), or 4 (Trainee).")]
        public short Role { get; set; }

        [Range(0, (double)decimal.MaxValue, ErrorMessage = "Target amount cannot be negative.")]
        public decimal Targetamount { get; set; } = 0;

        [Range(1, 2, ErrorMessage = "Status must be 1 (Active) or 2 (Inactive).")]
        public short Status { get; set; } = 1;
    }
}
