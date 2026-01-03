using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Commands;
using TenantCore.Application.Interfaces;

namespace TenantCore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscriptionPlansController : ControllerBase
{
    private readonly ISubscriptionPlanService _planService;

    public SubscriptionPlansController(ISubscriptionPlanService planService)
    {
        _planService = planService;
    }

    /// <summary>
    /// Get all active subscription plans (public)
    /// </summary>
    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<IActionResult> GetActive()
    {
        var plans = await _planService.GetActiveAsync();
        return Ok(plans);
    }

    /// <summary>
    /// Get all plans including inactive (SuperAdmin only)
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "RequireSuperAdmin")]
    public async Task<IActionResult> GetAll()
    {
        var plans = await _planService.GetAllAsync();
        return Ok(plans);
    }

    /// <summary>
    /// Get plan by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var plan = await _planService.GetByIdAsync(id);
        if (plan == null)
            return NotFound();

        return Ok(plan);
    }

    /// <summary>
    /// Create subscription plan (SuperAdmin only)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "RequireSuperAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateSubscriptionPlanCommand command)
    {
        var plan = await _planService.CreateAsync(command);
        return CreatedAtAction(nameof(GetById), new { id = plan.Id }, plan);
    }

    /// <summary>
    /// Update subscription plan (SuperAdmin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "RequireSuperAdmin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSubscriptionPlanCommand command)
    {
        try
        {
            var plan = await _planService.UpdateAsync(id, command);
            return Ok(plan);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Deactivate subscription plan (SuperAdmin only)
    /// </summary>
    [HttpPost("{id}/deactivate")]
    [Authorize(Policy = "RequireSuperAdmin")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var result = await _planService.DeactivateAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }
}
