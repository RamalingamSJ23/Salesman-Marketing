using MarketingApi.Models;
using MarketingApi.Repository;
using Microsoft.AspNetCore.Identity;

namespace MarketingApi.Service
{
    public class UsersService : IUsersService
    {
        private readonly IUsersRepository _usersRepository;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public UsersService(IUsersRepository usersRepository)
        {
            _usersRepository = usersRepository;
        }

        public async Task<List<Role>> GetRoles()
        {
            return await _usersRepository.GetRoles();
        }

        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            var user = await _usersRepository.GetUserByUsernameAsync(username.Trim());
            if (user == null)
            {
                return null;
            }

            var storedPassword = user.Passwordhash ?? string.Empty;
            var verificationResult = _passwordHasher.VerifyHashedPassword(user, storedPassword, password);
            var legacyMatch = string.Equals(storedPassword, password, StringComparison.Ordinal);

            if (verificationResult == PasswordVerificationResult.Failed && !legacyMatch)
            {
                return null;
            }

            if (verificationResult == PasswordVerificationResult.Failed && legacyMatch)
            {
                var rehashed = _passwordHasher.HashPassword(user, password);
                user.Passwordhash = rehashed;
            }

            return user;
        }
    }
}

