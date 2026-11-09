using MarketingApi.Models;
using MarketingApi.Repository;

namespace MarketingApi.Service
{
    public class UsersService : IUsersService
    {
        private readonly IUsersRepository _usersRepository;
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

            var user = await _usersRepository.GetUserByUsernameAsync(username);
            if (user == null)
            {
                return null;
            }

            var storedPassword = user.Passwordhash ?? string.Empty;
            if (!string.Equals(storedPassword.Trim(), password.Trim(), StringComparison.Ordinal))
            {
                return null;
            }

            return user;
        }
    }
}
