namespace ElevateWorkforce.Web.Services;

public sealed class JwtOptions
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "ElevateWorkforce";
    public string Audience { get; set; } = "ElevateWorkforce.Api";
    public int ExpirationMinutes { get; set; } = 60;
}
