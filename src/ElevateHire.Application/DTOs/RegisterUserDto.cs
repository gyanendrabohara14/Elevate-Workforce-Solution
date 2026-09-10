using System.ComponentModel.DataAnnotations;

namespace ElevateHire.Application.DTOs;

public sealed class RegisterUserDto
{
    [Required, StringLength(200, MinimumLength = 2)]
    public string FullName { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;

    [Required]
    public string Role { get; init; } = string.Empty;
}
