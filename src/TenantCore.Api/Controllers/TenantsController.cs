using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Commands;
using TenantCore.Application.Interfaces;

namespace TenantCore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;

    public TenantsController(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    /// <summary>
    /// Get all tenants (SuperAdmin only)
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "RequireSuperAdmin")]
    public async Task<IActionResult> GetAll()
    {
        var tenants = await _tenantService.GetAllAsync();
        return Ok(tenants);
    }

    /// <summary>
    /// Get tenant by ID (supports both cookie and API key authentication)
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(AuthenticationSchemes = "Identity.Application,ApiKey")]
    public async Task<IActionResult> GetById(Guid id)
    {
        // If authenticated via API key, verify the tenant ID matches
        if (User.HasClaim(c => c.Type == "AuthenticationType" && c.Value == "ApiKey"))
        {
            var authenticatedTenantId = User.FindFirst("TenantId")?.Value;
            if (authenticatedTenantId != id.ToString())
            {
                return Forbid("You can only access your own tenant information");
            }
        }

        var tenant = await _tenantService.GetByIdAsync(id);
        if (tenant == null)
            return NotFound();

        return Ok(tenant);
    }

    /// <summary>
    /// Get tenant by domain
    /// </summary>
    [HttpGet("domain/{domain}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByDomain(string domain)
    {
        var tenant = await _tenantService.GetByDomainAsync(domain);
        if (tenant == null)
            return NotFound();

        return Ok(tenant);
    }

    /// <summary>
    /// Create a new tenant (SuperAdmin only)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "RequireSuperAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateTenantCommand command)
    {
        var tenant = await _tenantService.CreateAsync(command);
        return CreatedAtAction(nameof(GetById), new { id = tenant.Id }, tenant);
    }

    /// <summary>
    /// Register a new tenant (Public)
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] CreateTenantCommand command)
    {
        try
        {
            var tenant = await _tenantService.CreateAsync(command);
            return CreatedAtAction(nameof(GetById), new { id = tenant.Id }, tenant);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Update tenant (SuperAdmin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "RequireSuperAdmin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTenantCommand command)
    {
        try
        {
            var tenant = await _tenantService.UpdateAsync(id, command);
            return Ok(tenant);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Deactivate tenant (SuperAdmin only)
    /// </summary>
    [HttpPost("{id}/deactivate")]
    [Authorize(Policy = "RequireSuperAdmin")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var result = await _tenantService.DeactivateAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Activate tenant (SuperAdmin only)
    /// </summary>
    [HttpPost("{id}/activate")]
    [Authorize(Policy = "RequireSuperAdmin")]
    public async Task<IActionResult> Activate(Guid id)
    {
        var result = await _tenantService.ActivateAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Delete tenant (SuperAdmin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "RequireSuperAdmin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _tenantService.DeleteAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }
}
