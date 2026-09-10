namespace ElevateHire.Infrastructure.Email;

public sealed class EmailSettings
{
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 1025;
    public string UserName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FromEmail { get; init; } = string.Empty;
    public string FromName { get; init; } = "ElevateHire";
    public bool UseSsl { get; init; }
}
