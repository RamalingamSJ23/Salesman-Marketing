using MarketingApi.Models;

namespace MarketingApi.Repository
{
    public interface IUsersRepository
    {
        public Task<List<Role>> GetRoles();
        public Task<User?> GetUserByUsernameAsync(string username);
    }
}
