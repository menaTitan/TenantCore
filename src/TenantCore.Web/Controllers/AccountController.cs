using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TenantCore.Application.Commands;
using TenantCore.Application.Interfaces;
using TenantCore.Infrastructure.Identity;

namespace TenantCore.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITenantService _tenantService;
    private readonly ISubscriptionPlanService _planService;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ITenantService tenantService,
        ISubscriptionPlanService planService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _tenantService = tenantService;
        _planService = planService;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginCommand model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName ?? user.Email,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            // Add custom claims
            var claims = new List<Claim>();
            if (user.TenantId.HasValue)
            {
                claims.Add(new Claim("TenantId", user.TenantId.Value.ToString()));
            }

            await _signInManager.SignInWithClaimsAsync(user, model.RememberMe, claims);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            // Redirect based on role
            if (user.IsSuperAdmin)
                return RedirectToAction("Dashboard", "SuperAdmin");
            else
                return RedirectToAction("Dashboard", "TenantAdmin");
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Account locked out.");
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
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Register()
    {
        ViewBag.Plans = await _planService.GetActiveAsync();
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(CreateTenantCommand command)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Plans = await _planService.GetActiveAsync();
            return View(command);
        }

        try
        {
            var tenant = await _tenantService.CreateAsync(command);

            // Auto-login the admin user
            var user = await _userManager.FindByEmailAsync(command.AdminEmail);
            if (user != null)
            {
                // Add custom claims including TenantId
                var claims = new List<Claim>();
                if (user.TenantId.HasValue)
                {
                    claims.Add(new Claim("TenantId", user.TenantId.Value.ToString()));
                }
                await _signInManager.SignInWithClaimsAsync(user, isPersistent: false, claims);
                return RedirectToAction("Dashboard", "TenantAdmin");
            }

            return RedirectToAction("Login");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewBag.Plans = await _planService.GetActiveAsync();
            return View(command);
        }
    }
}
