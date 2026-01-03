using FluentValidation;
using TenantCore.Application.Commands;

namespace TenantCore.Application.Validators;

public class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tenant name is required")
            .MaximumLength(100).WithMessage("Tenant name cannot exceed 100 characters");

        RuleFor(x => x.Domain)
            .NotEmpty().WithMessage("Domain is required")
            .MaximumLength(100).WithMessage("Domain cannot exceed 100 characters")
            .Matches(@"^[a-z0-9-]+$").WithMessage("Domain can only contain lowercase letters, numbers, and hyphens");

        RuleFor(x => x.BillingEmail)
            .NotEmpty().WithMessage("Billing email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.AdminFirstName)
            .NotEmpty().WithMessage("Admin first name is required")
            .MaximumLength(50).WithMessage("First name cannot exceed 50 characters");

        RuleFor(x => x.AdminLastName)
            .NotEmpty().WithMessage("Admin last name is required")
            .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters");

        RuleFor(x => x.AdminEmail)
            .NotEmpty().WithMessage("Admin email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.AdminPassword)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters");

        RuleFor(x => x.PlanId)
            .NotEmpty().WithMessage("Plan selection is required");
    }
}
