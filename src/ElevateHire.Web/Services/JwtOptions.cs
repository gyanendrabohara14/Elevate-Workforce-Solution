namespace ElevateWorkforce.Web.Services;

public sealed class JwtOptions
{
    public string Key { get; init; } = string.Empty;
    public string Issuer { get; init; } = "ElevateWorkforce";
    public string Audience { get; init; } = "ElevateWorkforce.Api";
    public int ExpirationMinutes { get; init; } = 60;
}
