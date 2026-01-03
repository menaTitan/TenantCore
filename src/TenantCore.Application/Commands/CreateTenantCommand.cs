namespace TenantCore.Application.Commands;

public class CreateTenantCommand
{
    public string Name { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string BillingEmail { get; set; } = string.Empty;
    public string? BillingAddress { get; set; }

    // Initial Admin User
    public string AdminFirstName { get; set; } = string.Empty;
    public string AdminLastName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;

    // Initial Subscription
    public Guid PlanId { get; set; }
}
