using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Controllers;

/// <summary>Register, log in, log out and the profile page. Everything is done by calling the API.</summary>
public class AccountController : Controller
{
    private readonly IRaceDayApiClient _api;

    public AccountController(IRaceDayApiClient api)
    {
        _api = api;
    }

    [HttpGet, AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToHome(User.IsInRole(AppRoles.Organiser) ? AppRoles.Organiser : AppRoles.Participant);
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var user = await _api.LoginAsync(model.Email, model.Password);
            await SignInAsync(user);
            TempData["Success"] = $"Welcome back, {user.FullName}!";

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return LocalRedirect(model.ReturnUrl);
            }

            return RedirectToHome(user.Role);
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError(string.Empty, ex.StatusCode == StatusCodes.Status401Unauthorized ? "Incorrect email or password." : ex.Message);
            return View(model);
        }
    }

    [HttpGet, AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new RegisterViewModel());
    }

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _api.RegisterAsync(model.FullName, model.Email, model.Password, model.PhoneNumber, model.Role);

            // Log the new user straight in so the API starts their session.
            var user = await _api.LoginAsync(model.Email, model.Password);
            await SignInAsync(user);
            TempData["Success"] = $"Welcome to RaceDay, {user.FullName}!";
            return RedirectToHome(user.Role);
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost, Authorize]
    public async Task<IActionResult> Logout()
    {
        try
        {
            await _api.LogoutAsync();
        }
        catch (ApiException)
        {
            // Even if the API is unreachable or the session already ended, log out here.
        }

        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["Success"] = "You have been logged out.";
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    [HttpGet, Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _api.GetProfileAsync(User.GetUserId());
        return View(ToProfileModel(user));
    }

    [HttpPost, Authorize]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            // Keep the read-only parts of the page filled in.
            var current = await _api.GetProfileAsync(User.GetUserId());
            model.Email = current.Email;
            model.Role = current.Role;
            model.ProfilePicture = current.ProfilePicture;
            return View(model);
        }

        var updated = await _api.UpdateProfileAsync(User.GetUserId(), model.FullName.Trim(), model.PhoneNumber?.Trim());
        await SignInAsync(updated);
        TempData["Success"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost, Authorize]
    public async Task<IActionResult> UploadProfilePicture(IFormFile? photo)
    {
        if (photo is null || photo.Length == 0)
        {
            TempData["Error"] = "Please choose a picture to upload first.";
            return RedirectToAction(nameof(Profile));
        }

        var updated = await _api.UploadProfilePictureAsync(User.GetUserId(), photo);
        await SignInAsync(updated);
        TempData["Success"] = "Your profile picture has been updated.";
        return RedirectToAction(nameof(Profile));
    }

    private static ProfileViewModel ToProfileModel(UserDto user) => new()
    {
        FullName = user.FullName,
        PhoneNumber = user.PhoneNumber,
        Email = user.Email,
        Role = user.Role,
        ProfilePicture = user.ProfilePicture
    };

    private IActionResult RedirectToHome(string role) =>
        role == AppRoles.Organiser ? RedirectToAction("Dashboard", "Organiser") : RedirectToAction("Index", "Home");

    /// <summary>Stores the user's id, name and role in the login cookie so every page knows who they are.</summary>
    private async Task SignInAsync(UserDto user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };

        if (!string.IsNullOrEmpty(user.ProfilePicture))
        {
            claims.Add(new Claim("ProfilePicture", user.ProfilePicture));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }
}
