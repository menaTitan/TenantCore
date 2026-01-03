using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Commands;
using TenantCore.Application.Interfaces;
using TenantCore.Infrastructure.Identity;

namespace TenantCore.Web.Controllers;

[Authorize(Policy = "RequireSuperAdmin")]
public class SuperAdminController : Controller
{
    private readonly ITenantService _tenantService;
    private readonly ISubscriptionPlanService _planService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IUserService _userService;
    private readonly UserManager<ApplicationUser> _userManager;

    public SuperAdminController(
        ITenantService tenantService,
        ISubscriptionPlanService planService,
        ISubscriptionService subscriptionService,
        IUserService userService,
        UserManager<ApplicationUser> userManager)
    {
        _tenantService = tenantService;
        _planService = planService;
        _subscriptionService = subscriptionService;
        _userService = userService;
        _userManager = userManager;
    }

    /// <summary>
    /// SuperAdmin Dashboard
    /// </summary>
    public async Task<IActionResult> Dashboard()
    {
        var tenants = await _tenantService.GetAllAsync();
        var plans = await _planService.GetAllAsync();

        ViewBag.TotalTenants = tenants.Count();
        ViewBag.ActiveTenants = tenants.Count(t => t.IsActive);
        ViewBag.TotalPlans = plans.Count();

        return View(tenants);
    }

    /// <summary>
    /// Tenant Management
    /// </summary>
    public async Task<IActionResult> Tenants()
    {
        var tenants = await _tenantService.GetAllAsync();
        return View(tenants);
    }

    /// <summary>
    /// Create Tenant
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CreateTenant()
    {
        var plans = await _planService.GetActiveAsync();
        ViewBag.Plans = plans;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CreateTenant(CreateTenantCommand command)
    {
        if (!ModelState.IsValid)
        {
            var plans = await _planService.GetActiveAsync();
            ViewBag.Plans = plans;
            return View(command);
        }

        await _tenantService.CreateAsync(command);
        TempData["Success"] = "Tenant created successfully";
        return RedirectToAction(nameof(Tenants));
    }

    /// <summary>
    /// Subscription Plans Management
    /// </summary>
    public async Task<IActionResult> Plans()
    {
        var plans = await _planService.GetAllAsync();
        return View(plans);
    }

    /// <summary>
    /// Create Plan
    /// </summary>
    [HttpGet]
    public IActionResult CreatePlan()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CreatePlan(CreateSubscriptionPlanCommand command)
    {
        if (!ModelState.IsValid)
            return View(command);

        await _planService.CreateAsync(command);
        TempData["Success"] = "Plan created successfully";
        return RedirectToAction(nameof(Plans));
    }

    /// <summary>
    /// View Expired Subscriptions
    /// </summary>
    public async Task<IActionResult> ExpiredSubscriptions()
    {
        var expiredSubs = await _subscriptionService.GetExpiredSubscriptionsAsync();
        return View(expiredSubs);
    }

    /// <summary>
    /// User Management - List all users across all tenants
    /// </summary>
    public async Task<IActionResult> Users()
    {
        var allUsers = new List<object>();
        var tenants = await _tenantService.GetAllAsync();

        foreach (var tenant in tenants)
        {
            var users = await _userService.GetUsersByTenantAsync(tenant.Id);
            foreach (var user in users)
            {
                allUsers.Add(new
                {
                    UserId = user.Id,
                    UserName = user.Email,
                    Email = user.Email,
                    TenantId = tenant.Id,
                    TenantName = tenant.Name,
                    Roles = user.Roles
                });
            }
        }

        ViewBag.Tenants = tenants;
        return View(allUsers);
    }

    /// <summary>
    /// Create User (SuperAdmin can create users for any tenant)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CreateUser()
    {
        var tenants = await _tenantService.GetAllAsync();
        ViewBag.Tenants = tenants;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(Guid tenantId, CreateUserCommand command)
    {
        if (!ModelState.IsValid)
        {
            var tenants = await _tenantService.GetAllAsync();
            ViewBag.Tenants = tenants;
            return View(command);
        }

        try
        {
            await _userService.CreateUserAsync(tenantId, command);
            TempData["Success"] = "User created successfully";
            return RedirectToAction(nameof(Users));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            var tenants = await _tenantService.GetAllAsync();
            ViewBag.Tenants = tenants;
            return View(command);
        }
    }

    /// <summary>
    /// Map User to Different Tenant
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> MapUserToTenant(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return NotFound();

        var tenants = await _tenantService.GetAllAsync();
        ViewBag.Tenants = tenants;
        ViewBag.User = user;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> MapUserToTenant(Guid userId, Guid tenantId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return NotFound();

        user.TenantId = tenantId;
        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            TempData["Success"] = "User mapped to tenant successfully";
            return RedirectToAction(nameof(Users));
        }

        TempData["Error"] = "Failed to map user to tenant";
        return RedirectToAction(nameof(Users));
    }

    /// <summary>
    /// Reset User Password
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ResetUserPassword(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return NotFound();

        ViewBag.User = user;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ResetUserPassword(Guid userId, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            TempData["Error"] = "Password must be at least 6 characters long";
            return RedirectToAction(nameof(ResetUserPassword), new { userId });
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return NotFound();

        // Remove current password
        var removeResult = await _userManager.RemovePasswordAsync(user);
        if (!removeResult.Succeeded)
        {
            TempData["Error"] = "Failed to reset password";
            return RedirectToAction(nameof(Users));
        }

        // Add new password
        var addResult = await _userManager.AddPasswordAsync(user, newPassword);
        if (addResult.Succeeded)
        {
            TempData["Success"] = $"Password reset successfully for {user.Email}";
            return RedirectToAction(nameof(Users));
        }

        TempData["Error"] = string.Join(", ", addResult.Errors.Select(e => e.Description));
        return RedirectToAction(nameof(Users));
    }

    /// <summary>
    /// Delete User
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> DeleteUser(Guid userId)
    {
        try
        {
            await _userService.DeleteUserAsync(userId);
            TempData["Success"] = "User deleted successfully";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Users));
    }

    /// <summary>
    /// Deactivate Tenant
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> DeactivateTenant(Guid tenantId)
    {
        try
        {
            await _tenantService.DeactivateAsync(tenantId);
            TempData["Success"] = "Tenant deactivated successfully";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Tenants));
    }

}
