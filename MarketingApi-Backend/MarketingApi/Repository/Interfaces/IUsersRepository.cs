using MarketingApi.Models.Entities;

namespace MarketingApi.Repository.Interfaces;

public interface IUsersRepository
{
    Task<List<Role>> GetRoles();
    Task<User?> GetUserByUsernameAsync(string username, bool trackChanges = false);
    Task SaveChangesAsync();
}
