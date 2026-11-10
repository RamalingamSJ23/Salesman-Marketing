using MarketingApi.Models.Entities;

namespace MarketingApi.Service.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(User user);
}
