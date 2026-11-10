namespace MarketingApi.Options;

public sealed class JwtOptions
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "MarketingApi";
    public string Audience { get; set; } = "MarketingApiClients";
    public int ExpireHours { get; set; } = 8;
}
