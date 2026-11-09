using MarketingApi.Data;
using MarketingApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketingApi.Repository
{
    public class UsersRepository :IUsersRepository
    {
        private readonly SalesMarketingDbContext _dbContext;
        public UsersRepository(SalesMarketingDbContext salesMarketingDbContext)
        {
            _dbContext = salesMarketingDbContext;
        }
        public async Task<List<Role>> GetRoles()
        {
            var roles = await _dbContext.Roles.ToListAsync();
            
            return roles;
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _dbContext.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.Trim().ToLower());
        }
    }
}
