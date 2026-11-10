using MarketingApi.Models.Entities;

namespace MarketingApi.Service.Interfaces;

public interface IUsersService
{
    Task<List<Role>> GetRoles();
    Task<User?> AuthenticateAsync(string username, string password);
}
