using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Commands;
using TenantCore.Application.Interfaces;

namespace TenantCore.Web.Controllers;

[Authorize(Policy = "RequireTenantAdmin")]
public class TenantAdminController : Controller
{
    private readonly ITenantProvider _tenantProvider;
    private readonly ITenantService _tenantService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ISubscriptionPlanService _planService;
    private readonly IUserService _userService;

    public TenantAdminController(
        ITenantProvider tenantProvider,
        ITenantService tenantService,
        ISubscriptionService subscriptionService,
        ISubscriptionPlanService planService,
        IUserService userService)
    {
        _tenantProvider = tenantProvider;
        _tenantService = tenantService;
        _subscriptionService = subscriptionService;
        _planService = planService;
        _userService = userService;
    }

    /// <summary>
    /// Manage Users
    /// </summary>
    public async Task<IActionResult> Users(Guid? tenantId = null)
    {
        // SuperAdmin can view any tenant by passing tenantId
        if (User.IsInRole("SuperAdmin") && tenantId.HasValue)
        {
            // Allow SuperAdmin to view specific tenant
        }
        else
        {
            tenantId = _tenantProvider.CurrentTenantId;
        }

        if (tenantId == null)
        {
            if (User.IsInRole("SuperAdmin"))
                return RedirectToAction("Tenants", "SuperAdmin");

            return Forbid();
        }

        var users = await _userService.GetUsersByTenantAsync(tenantId.Value);
        ViewBag.TenantId = tenantId;
        return View(users);
    }

    [HttpGet]
    public IActionResult CreateUser(Guid? tenantId = null)
    {
        // SuperAdmin can create users for any tenant
        if (User.IsInRole("SuperAdmin") && tenantId.HasValue)
        {
            ViewBag.TenantId = tenantId;
        }
        else
        {
            tenantId = _tenantProvider.CurrentTenantId;
            ViewBag.TenantId = tenantId;
        }

        if (tenantId == null)
        {
            if (User.IsInRole("SuperAdmin"))
                return RedirectToAction("Tenants", "SuperAdmin");

            return Forbid();
        }

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(CreateUserCommand command, Guid? tenantId = null)
    {
        // SuperAdmin can create users for any tenant
        if (User.IsInRole("SuperAdmin") && tenantId.HasValue)
        {
            // Allow SuperAdmin to create for specific tenant
        }
        else
        {
            tenantId = _tenantProvider.CurrentTenantId;
        }

        if (tenantId == null)
        {
            if (User.IsInRole("SuperAdmin"))
                return RedirectToAction("Tenants", "SuperAdmin");

            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.TenantId = tenantId;
            return View(command);
        }

        try
        {
            await _userService.CreateUserAsync(tenantId.Value, command);
            TempData["Success"] = "User created successfully";
            return RedirectToAction(nameof(Users), new { tenantId });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.TenantId = tenantId;
            return View(command);
        }
    }

    /// <summary>
    /// Tenant Admin Dashboard
    /// </summary>
    public async Task<IActionResult> Dashboard(Guid? tenantId = null)
    {
        // SuperAdmin can view any tenant by passing tenantId
        if (User.IsInRole("SuperAdmin") && tenantId.HasValue)
        {
            // Allow SuperAdmin to view specific tenant
        }
        else
        {
            tenantId = _tenantProvider.CurrentTenantId;
        }

        if (tenantId == null)
        {
            // If SuperAdmin without tenant selection, redirect to tenant list
            if (User.IsInRole("SuperAdmin"))
                return RedirectToAction("Tenants", "SuperAdmin");

            return Forbid();
        }

        var tenant = await _tenantService.GetByIdAsync(tenantId.Value);
        var subscription = await _subscriptionService.GetActiveTenantSubscriptionAsync(tenantId.Value);

        ViewBag.Subscription = subscription;
        ViewBag.TenantId = tenantId;

        return View(tenant);
    }

    /// <summary>
    /// Manage Subscription
    /// </summary>
    public async Task<IActionResult> Subscription(Guid? tenantId = null)
    {
        // SuperAdmin can view any tenant by passing tenantId
        if (User.IsInRole("SuperAdmin") && tenantId.HasValue)
        {
            // Allow SuperAdmin to view specific tenant
        }
        else
        {
            tenantId = _tenantProvider.CurrentTenantId;
        }

        if (tenantId == null)
        {
            if (User.IsInRole("SuperAdmin"))
                return RedirectToAction("Tenants", "SuperAdmin");

            return Forbid();
        }

        var subscription = await _subscriptionService.GetActiveTenantSubscriptionAsync(tenantId.Value);
        var plans = await _planService.GetActiveAsync();

        ViewBag.Plans = plans;
        ViewBag.TenantId = tenantId;

        return View(subscription);
    }

    /// <summary>
    /// Upgrade Subscription
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Upgrade(Guid? tenantId = null)
    {
        // SuperAdmin can upgrade any tenant
        if (User.IsInRole("SuperAdmin") && tenantId.HasValue)
        {
            ViewBag.TenantId = tenantId;
        }
        else
        {
            tenantId = _tenantProvider.CurrentTenantId;
            ViewBag.TenantId = tenantId;
        }

        if (tenantId == null)
        {
            if (User.IsInRole("SuperAdmin"))
                return RedirectToAction("Tenants", "SuperAdmin");

            return Forbid();
        }

        var plans = await _planService.GetActiveAsync();
        return View(plans);
    }

    [HttpPost]
    public async Task<IActionResult> Upgrade(UpgradeSubscriptionCommand command, Guid? tenantId = null)
    {
        // SuperAdmin can upgrade any tenant
        if (User.IsInRole("SuperAdmin") && tenantId.HasValue)
        {
            // Allow SuperAdmin to upgrade for specific tenant
        }
        else
        {
            tenantId = _tenantProvider.CurrentTenantId;
        }

        if (tenantId == null)
        {
            if (User.IsInRole("SuperAdmin"))
                return RedirectToAction("Tenants", "SuperAdmin");

            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.TenantId = tenantId;
            var plans = await _planService.GetActiveAsync();
            return View(plans);
        }

        await _subscriptionService.UpgradeAsync(tenantId.Value, command);
        TempData["Success"] = "Subscription upgraded successfully";
        return RedirectToAction(nameof(Subscription), new { tenantId });
    }

    /// <summary>
    /// Cancel Subscription
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CancelSubscription(Guid? tenantId = null)
    {
        // SuperAdmin can cancel any tenant's subscription
        if (User.IsInRole("SuperAdmin") && tenantId.HasValue)
        {
            // Allow SuperAdmin to cancel for specific tenant
        }
        else
        {
            tenantId = _tenantProvider.CurrentTenantId;
        }

        if (tenantId == null)
        {
            if (User.IsInRole("SuperAdmin"))
                return RedirectToAction("Tenants", "SuperAdmin");

            return Forbid();
        }

        var subscription = await _subscriptionService.GetActiveTenantSubscriptionAsync(tenantId.Value);
        if (subscription != null)
        {
            await _subscriptionService.CancelAsync(subscription.Id);
            TempData["Success"] = "Subscription cancelled successfully";
        }

        return RedirectToAction(nameof(Subscription), new { tenantId });
    }

    /// <summary>
    /// Tenant Settings
    /// </summary>
    public async Task<IActionResult> Settings(Guid? tenantId = null)
    {
        // SuperAdmin can view any tenant by passing tenantId
        if (User.IsInRole("SuperAdmin") && tenantId.HasValue)
        {
            // Allow SuperAdmin to view specific tenant
        }
        else
        {
            tenantId = _tenantProvider.CurrentTenantId;
        }

        if (tenantId == null)
        {
            if (User.IsInRole("SuperAdmin"))
                return RedirectToAction("Tenants", "SuperAdmin");

            return Forbid();
        }

        var tenant = await _tenantService.GetByIdAsync(tenantId.Value);
        ViewBag.TenantId = tenantId;
        return View(tenant);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateSettings(UpdateTenantCommand command, Guid? tenantId = null)
    {
        // SuperAdmin can update settings for any tenant
        if (User.IsInRole("SuperAdmin") && tenantId.HasValue)
        {
            // Allow SuperAdmin to update for specific tenant
        }
        else
        {
            tenantId = _tenantProvider.CurrentTenantId;
        }

        if (tenantId == null)
        {
            if (User.IsInRole("SuperAdmin"))
                return RedirectToAction("Tenants", "SuperAdmin");

            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.TenantId = tenantId;
            var tenant = await _tenantService.GetByIdAsync(tenantId.Value);
            return View("Settings", tenant);
        }

        try
        {
            await _tenantService.UpdateAsync(tenantId.Value, command);
            TempData["Success"] = "Settings updated successfully";
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.TenantId = tenantId;
            var tenant = await _tenantService.GetByIdAsync(tenantId.Value);
            return View("Settings", tenant);
        }

        return RedirectToAction(nameof(Settings), new { tenantId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteUser(Guid id, Guid? tenantId = null)
    {
        // SuperAdmin can delete users from any tenant
        if (User.IsInRole("SuperAdmin") && tenantId.HasValue)
        {
            // Allow SuperAdmin to delete for specific tenant
        }
        else
        {
            tenantId = _tenantProvider.CurrentTenantId;
        }

        if (tenantId == null)
        {
            if (User.IsInRole("SuperAdmin"))
                return RedirectToAction("Tenants", "SuperAdmin");

            return Forbid();
        }

        await _userService.DeleteUserAsync(id);
        TempData["Success"] = "User deleted successfully";
        return RedirectToAction(nameof(Users), new { tenantId });
    }

    /// <summary>
    /// Reports Dashboard
    /// </summary>
    public IActionResult Reports()
    {
        return View();
    }

    /// <summary>
    /// API Key Management
    /// </summary>
    public async Task<IActionResult> ApiKey(Guid? tenantId = null)
    {
        // SuperAdmin can view any tenant by passing tenantId
        if (User.IsInRole("SuperAdmin") && tenantId.HasValue)
        {
            // Allow SuperAdmin to view specific tenant
        }
        else
        {
            tenantId = _tenantProvider.CurrentTenantId;
        }

        if (tenantId == null)
        {
            if (User.IsInRole("SuperAdmin"))
                return RedirectToAction("Tenants", "SuperAdmin");

            return Forbid();
        }

        var tenant = await _tenantService.GetByIdAsync(tenantId.Value);
        if (tenant == null)
            return NotFound();

        return View(tenant);
    }

    /// <summary>
    /// Regenerate API Key
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> RegenerateApiKey(Guid? tenantId = null)
    {
        // SuperAdmin can regenerate for any tenant
        if (User.IsInRole("SuperAdmin") && tenantId.HasValue)
        {
            // Allow SuperAdmin to regenerate for specific tenant
        }
        else
        {
            tenantId = _tenantProvider.CurrentTenantId;
        }

        if (tenantId == null)
        {
            if (User.IsInRole("SuperAdmin"))
                return RedirectToAction("Tenants", "SuperAdmin");

            return Forbid();
        }

        try
        {
            var newApiKey = await _tenantService.RegenerateApiKeyAsync(tenantId.Value);
            TempData["Success"] = "API key regenerated successfully";
            TempData["NewApiKey"] = newApiKey;
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to regenerate API key: {ex.Message}";
        }

        return RedirectToAction(nameof(ApiKey), new { tenantId });
    }
}
