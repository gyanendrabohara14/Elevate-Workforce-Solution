namespace ElevateHire.Web.Services;

public sealed class JwtOptions
{
    public string Key { get; init; } = string.Empty;
    public string Issuer { get; init; } = "ElevateHire";
    public string Audience { get; init; } = "ElevateHire.Api";
    public int ExpirationMinutes { get; init; } = 60;
}
