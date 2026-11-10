namespace MarketingApi.DTOs.Auth
{
    /// <summary>
    /// Request body for login.
    /// </summary>
    public class LoginDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response after successful login containing user info and role.
    /// </summary>
    public class LoginResponseDto
    {
        public long Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public long Roleid { get; set; }
        public string Rolename { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }
}
