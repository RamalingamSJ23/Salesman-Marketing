using MarketingApi.Models.Entities;
using MarketingApi.Repository.Interfaces;
using MarketingApi.Service.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace MarketingApi.Service.Implementations;

public class UsersService : IUsersService
{
    private readonly IUsersRepository _usersRepository;
    private readonly ILogger<UsersService> _logger;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public UsersService(IUsersRepository usersRepository, ILogger<UsersService> logger)
    {
        _usersRepository = usersRepository;
        _logger = logger;
    }

    public async Task<List<Role>> GetRoles()
    {
        return await _usersRepository.GetRoles();
    }

    public async Task<User?> AuthenticateAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("Authentication rejected due to empty username or password.");
            return null;
        }

        var cleanUsername = username.Trim();
        var user = await _usersRepository.GetUserByUsernameAsync(cleanUsername, trackChanges: true);
        if (user == null)
        {
            _logger.LogWarning("Failed login attempt for unknown user {Username}.", cleanUsername);
            return null;
        }

        var storedPassword = user.PasswordHash ?? string.Empty;
        var verificationResult = PasswordVerificationResult.Failed;

        // PasswordHasher.VerifyHashedPassword throws FormatException if the stored value is legacy plain text
        try
        {
            verificationResult = _passwordHasher.VerifyHashedPassword(user, storedPassword, password);
        }
        catch (FormatException)
        {
            verificationResult = PasswordVerificationResult.Failed;
        }

        if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, password);
            await _usersRepository.SaveChangesAsync();
            _logger.LogInformation("User {Username} password hash upgraded on login.", user.Username);
            return user;
        }

        if (verificationResult == PasswordVerificationResult.Success)
        {
            _logger.LogInformation("User {Username} authenticated successfully.", user.Username);
            return user;
        }

        // Legacy plaintext fallback: upgrade to salted PBKDF2 hash and persist immediately
        if (string.Equals(storedPassword, password, StringComparison.Ordinal))
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, password);
            await _usersRepository.SaveChangesAsync();
            _logger.LogInformation("User {Username} legacy password hashed and persisted on login.", user.Username);
            return user;
        }

        _logger.LogWarning("Failed login attempt for user {Username}.", user.Username);
        return null;
    }
}
