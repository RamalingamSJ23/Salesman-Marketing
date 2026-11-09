using System.ComponentModel.DataAnnotations;

namespace MarketingApi.DTOs.Salesman
{
    /// <summary>
    /// Input DTO for creating a new Salesman.
    /// </summary>
    public class CreateSalesmanDto
    {
        [Required(ErrorMessage = "Salesman code is required.")]
        [MaxLength(20, ErrorMessage = "Salesman code cannot exceed 20 characters.")]
        public string Salesmancode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Salesman name is required.")]
        [MaxLength(100, ErrorMessage = "Salesman name cannot exceed 100 characters.")]
        public string Salesmanname { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mobile number is required.")]
        [RegularExpression(@"^[0-9]{7,15}$", ErrorMessage = "Mobile number must be 7 to 15 digits.")]
        [MaxLength(20, ErrorMessage = "Mobile number cannot exceed 20 characters.")]
        public string Mobileno { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [MaxLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Joining date is required.")]
        public DateOnly? Joiningdate { get; set; }

        public short Status { get; set; } = 1; // Default: Active (1)
    }
}
