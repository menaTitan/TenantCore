namespace TenantCore.Application.Commands;

public class RegisterTenantCommand
{
    public string TenantName { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public Guid? PlanId { get; set; }
}
