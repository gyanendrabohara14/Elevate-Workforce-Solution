namespace ElevateWorkforce.Application.DTOs;

public sealed record RegisterUserResponseDto(int UserId, string FullName, string Email, string Role, string Token);
