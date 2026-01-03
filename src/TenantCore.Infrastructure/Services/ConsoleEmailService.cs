using Microsoft.Extensions.Logging;
using TenantCore.Application.Interfaces;

namespace TenantCore.Infrastructure.Services;

public class ConsoleEmailService : IEmailService
{
    private readonly ILogger<ConsoleEmailService> _logger;

    public ConsoleEmailService(ILogger<ConsoleEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string to, string subject, string body)
    {
        _logger.LogInformation("===== EMAIL =====");
        _logger.LogInformation("To: {To}", to);
        _logger.LogInformation("Subject: {Subject}", subject);
        _logger.LogInformation("Body: {Body}", body);
        _logger.LogInformation("=================");
        return Task.CompletedTask;
    }

    public Task SendWelcomeEmailAsync(string to, string tenantName)
    {
        var subject = $"Welcome to {tenantName}!";
        var body = $"Welcome to TenantCore! Your organization '{tenantName}' has been successfully created.";
        return SendEmailAsync(to, subject, body);
    }

    public Task SendSubscriptionExpiringEmailAsync(string to, string tenantName, int daysRemaining)
    {
        var subject = $"Subscription Expiring Soon - {tenantName}";
        var body = $"Your subscription for '{tenantName}' will expire in {daysRemaining} days.";
        return SendEmailAsync(to, subject, body);
    }

    public Task SendSubscriptionExpiredEmailAsync(string to, string tenantName)
    {
        var subject = $"Subscription Expired - {tenantName}";
        var body = $"Your subscription for '{tenantName}' has expired.";
        return SendEmailAsync(to, subject, body);
    }

    public Task SendPaymentFailedEmailAsync(string to, string tenantName)
    {
        var subject = $"Payment Failed - {tenantName}";
        var body = $"We were unable to process your payment for '{tenantName}'.";
        return SendEmailAsync(to, subject, body);
    }
}
