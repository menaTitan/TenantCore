namespace TenantCore.Application.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
    Task SendWelcomeEmailAsync(string to, string tenantName);
    Task SendSubscriptionExpiringEmailAsync(string to, string tenantName, int daysRemaining);
    Task SendSubscriptionExpiredEmailAsync(string to, string tenantName);
    Task SendPaymentFailedEmailAsync(string to, string tenantName);
}
