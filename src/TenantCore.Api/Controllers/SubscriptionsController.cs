using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Commands;
using TenantCore.Application.Interfaces;
using TenantCore.Domain.Enums;

namespace TenantCore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ITenantProvider _tenantProvider;

    public SubscriptionsController(
        ISubscriptionService subscriptionService,
        ITenantProvider tenantProvider)
    {
        _subscriptionService = subscriptionService;
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// Get current tenant's active subscription
    /// </summary>
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrent()
    {
        var tenantId = _tenantProvider.CurrentTenantId;
        if (tenantId == null)
            return BadRequest("No tenant context");

        var subscription = await _subscriptionService.GetActiveTenantSubscriptionAsync(tenantId.Value);
        if (subscription == null)
            return NotFound();

        return Ok(subscription);
    }

    /// <summary>
    /// Get subscription by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var subscription = await _subscriptionService.GetByIdAsync(id);
        if (subscription == null)
            return NotFound();

        return Ok(subscription);
    }

    /// <summary>
    /// Get tenant's subscription history
    /// </summary>
    [HttpGet("tenant/{tenantId}")]
    public async Task<IActionResult> GetTenantSubscriptions(Guid tenantId)
    {
        var subscriptions = await _subscriptionService.GetTenantSubscriptionsAsync(tenantId);
        return Ok(subscriptions);
    }

    /// <summary>
    /// Upgrade subscription
    /// </summary>
    [HttpPost("upgrade")]
    [Authorize(Policy = "RequireTenantAdmin")]
    public async Task<IActionResult> Upgrade([FromBody] UpgradeSubscriptionCommand command)
    {
        var tenantId = _tenantProvider.CurrentTenantId;
        if (tenantId == null)
            return BadRequest("No tenant context");

        var subscription = await _subscriptionService.UpgradeAsync(tenantId.Value, command);
        return Ok(subscription);
    }

    /// <summary>
    /// Cancel subscription
    /// </summary>
    [HttpPost("{id}/cancel")]
    [Authorize(Policy = "RequireTenantAdmin")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var result = await _subscriptionService.CancelAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Get expired subscriptions (SuperAdmin only)
    /// </summary>
    [HttpGet("expired")]
    [Authorize(Policy = "RequireSuperAdmin")]
    public async Task<IActionResult> GetExpired()
    {
        var subscriptions = await _subscriptionService.GetExpiredSubscriptionsAsync();
        return Ok(subscriptions);
    }
}
