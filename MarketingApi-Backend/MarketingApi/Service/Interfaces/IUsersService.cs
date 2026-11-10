using MarketingApi.Models;

namespace MarketingApi.Service
{
    public interface IUsersService
    {
        public Task<List<Role>> GetRoles();
        public Task<User?> AuthenticateAsync(string username, string password);
    }
}
