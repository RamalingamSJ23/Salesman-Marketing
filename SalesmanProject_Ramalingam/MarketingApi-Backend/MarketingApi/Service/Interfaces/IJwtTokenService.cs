using MarketingApi.Models;

namespace MarketingApi.Service
{
    public interface IJwtTokenService
    {
        string GenerateToken(User user);
    }
}
