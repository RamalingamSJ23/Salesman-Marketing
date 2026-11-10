using MarketingApi.Data;
using MarketingApi.Models.Entities;
using MarketingApi.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MarketingApi.Repository.Implementations;

public class UsersRepository : IUsersRepository
{
    private readonly SalesMarketingDbContext _dbContext;

    public UsersRepository(SalesMarketingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Role>> GetRoles()
    {
        return await _dbContext.Roles
            .AsNoTracking()
            .OrderBy(r => r.Id)
            .ToListAsync();
    }

    public async Task<User?> GetUserByUsernameAsync(string username, bool trackChanges = false)
    {
        var cleanUsername = username.Trim();
        var query = _dbContext.Users
            .Include(u => u.Role)
            .AsQueryable();

        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(u => EF.Functions.ILike(u.Username, cleanUsername));
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
}
