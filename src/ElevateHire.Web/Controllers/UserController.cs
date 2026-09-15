using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ElevateWorkforce.Application.DTOs;
using ElevateWorkforce.Application.Interfaces;
using ElevateWorkforce.Domain.Entities;
using ElevateWorkforce.Domain.Enums;
using ElevateWorkforce.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ElevateWorkforce.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class UserController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IUserService _userService;
    private readonly ICacheService _cacheService;
    private readonly IEmailService _emailService;
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<UserController> _logger;

    public UserController(
        UserManager<User> userManager,
        RoleManager<ApplicationRole> roleManager,
        IUserService userService,
        ICacheService cacheService,
        IEmailService emailService,
        IOptions<JwtOptions> jwtOptions,
        ILogger<UserController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _userService = userService;
        _cacheService = cacheService;
        _emailService = emailService;
        _jwtOptions = jwtOptions.Value;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(RegisterUserResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterUserDto request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<UserRole>(request.Role, true, out var role) || role == UserRole.Admin)
            return BadRequest(new { message = "Role must be Employer or JobSeeker." });

        var email = request.Email.Trim();
        if (await _userManager.FindByEmailAsync(email) is not null)
            return Conflict(new { message = "An account with this email already exists." });

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            UserName = email,
            Role = role
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            return BadRequest(new { errors = createResult.Errors.Select(error => error.Description) });

        if (!await _roleManager.RoleExistsAsync(role.ToString()))
        {
            await _userManager.DeleteAsync(user);
            return BadRequest(new { message = $"Role '{role}' is not configured." });
        }

        var roleResult = await _userManager.AddToRoleAsync(user, role.ToString());
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return BadRequest(new { errors = roleResult.Errors.Select(error => error.Description) });
        }

        if (role == UserRole.Employer)
            await _userService.EnsureEmployerAsync(user);
        else
            await _userService.EnsureJobSeekerAsync(user);

        var response = new RegisterUserResponseDto(user.Id, user.FullName, user.Email, role.ToString(), CreateToken(user, role));
        try
        {
            await _cacheService.SetAsync($"users:{user.Id}", response, TimeSpan.FromMinutes(15), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "User cache could not be updated for {UserId}.", user.Id);
        }

        try
        {
            await _emailService.SendWelcomeEmailAsync(user.Email, user.FullName, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Welcome email could not be sent for {Email}.", user.Email);
        }

        return CreatedAtAction(nameof(Register), new { id = user.Id }, response);
    }

    private string CreateToken(User user, UserRole role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(ClaimTypes.Role, role.ToString())
        };
        var token = new JwtSecurityToken(
            _jwtOptions.Issuer,
            _jwtOptions.Audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
