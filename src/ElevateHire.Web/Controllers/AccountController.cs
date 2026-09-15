using ElevateWorkforce.Domain.Entities;
using ElevateWorkforce.Domain.Enums;
using ElevateWorkforce.Web.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ElevateWorkforce.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly ElevateWorkforce.Application.Interfaces.IUserService _userService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        ElevateWorkforce.Application.Interfaces.IUserService userService,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _userService = userService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult GetStarted() => View();

    [HttpGet]
    public IActionResult Register(string? role = null)
    {
        var model = new RegisterViewModel();
        if (role?.Trim() == "Employer")
            model.Role = "Employer";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var role = model.Role == "Employer" ? UserRole.Employer : UserRole.JobSeeker;
        var user = new User
        {
            FullName = model.FullName.Trim(),
            Email = model.Email.Trim(),
            UserName = model.Email.Trim(),
            Role = role
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        var roleResult = await _userManager.AddToRoleAsync(user, role.ToString());
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            foreach (var error in roleResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        _logger.LogInformation("User {Email} registered as {Role}.", user.Email, role);

        if (role == UserRole.Employer)
        {
            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Company", "Employer", new { firstTime = "true" });
        }

        await _userService.EnsureJobSeekerAsync(user);
        await _signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToAction("Profile", "JobSeeker", new { firstTime = "true" });
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null, string? role = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToRoleHome();

        return View(new LoginViewModel
        {
            ReturnUrl = returnUrl,
            Role = role?.Trim() switch
            {
                "Employer" => "Employer",
                "Admin" => "Admin",
                _ => "JobSeeker"
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user is not null)
        {
            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "This account has been disabled. Contact an administrator.");
                return View(model);
            }
        }

        var result = await _signInManager.PasswordSignInAsync(
            model.Email.Trim(), model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            var roleName = model.Role?.Trim();
            if (roleName is not null && (user?.Role.ToString() ?? string.Empty) != roleName)
            {
                await _signInManager.SignOutAsync();
                ModelState.AddModelError(string.Empty,
                    $"This account is signed up as a different role. Select the correct account type above and try again.");
                model.Password = string.Empty;
                return View(model);
            }

            if (user is not null)
            {
                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
            }

            _logger.LogInformation("User {Email} logged in.", model.Email);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            return RedirectToRoleHome();
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Too many attempts. Your account is temporarily locked.");
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out.");
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    private IActionResult RedirectToRoleHome()
    {
        if (User?.IsInRole("Admin") == true) return RedirectToAction("Dashboard", "Admin");
        if (User?.IsInRole("Employer") == true) return RedirectToAction("Dashboard", "Employer");
        if (User?.IsInRole("JobSeeker") == true) return RedirectToAction("Dashboard", "JobSeeker");
        return RedirectToAction("Index", "Home");
    }
}