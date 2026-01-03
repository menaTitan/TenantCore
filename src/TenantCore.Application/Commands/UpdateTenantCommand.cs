namespace TenantCore.Application.Commands;

public class UpdateTenantCommand
{
    public string Name { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string? BillingEmail { get; set; }
    public string? BillingAddress { get; set; }
}
