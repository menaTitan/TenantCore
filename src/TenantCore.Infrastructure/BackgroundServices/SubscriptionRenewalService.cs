using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Interfaces;
using TenantCore.Domain.Enums;

namespace TenantCore.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that checks for expired subscriptions and attempts to renew them
/// </summary>
public class SubscriptionRenewalService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SubscriptionRenewalService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(6); // Run every 6 hours

    public SubscriptionRenewalService(
        IServiceProvider serviceProvider,
        ILogger<SubscriptionRenewalService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Subscription Renewal Service is starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredSubscriptions();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing expired subscriptions");
            }

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Expected when service is stopping
                break;
            }
        }

        _logger.LogInformation("Subscription Renewal Service is stopping");
    }

    private async Task ProcessExpiredSubscriptions()
    {
        _logger.LogInformation("Starting subscription renewal check at {Time}", DateTime.UtcNow);

        using var scope = _serviceProvider.CreateScope();
        var subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
        var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();

        var expiredSubscriptions = await subscriptionService.GetExpiredSubscriptionsAsync();

        foreach (var subscription in expiredSubscriptions)
        {
            try
            {
                if (subscription.AutoRenew)
                {
                    _logger.LogInformation(
                        "Processing auto-renewal for subscription {SubscriptionId} (Tenant: {TenantId})",
                        subscription.Id, subscription.TenantId);

                    // Attempt to charge the customer
                    // Note: In a real app, we'd get the CustomerId from the Tenant or User entity
                    var customerId = "cus_mock"; 
                    var amount = 100m; // Should get from Plan
                    var currency = "usd";

                    var paymentSuccess = await paymentService.ChargeAsync(customerId, amount, currency);

                    if (paymentSuccess)
                    {
                        // Renew subscription
                        // Note: SubscriptionService needs a RenewAsync method, but for now we'll just log
                        _logger.LogInformation("Payment successful. Renewing subscription {SubscriptionId}", subscription.Id);
                        
                        // Assuming RenewAsync exists or we manually update dates
                        // await subscriptionService.RenewAsync(subscription.Id); 
                    }
                    else
                    {
                        await subscriptionService.UpdateStatusAsync(subscription.Id, SubscriptionStatus.PastDue);
                        _logger.LogWarning("Payment failed for subscription {SubscriptionId}", subscription.Id);
                    }
                }
                else
                {
                    // Mark as expired if not set to auto-renew
                    await subscriptionService.UpdateStatusAsync(subscription.Id, SubscriptionStatus.Expired);

                    _logger.LogInformation(
                        "Subscription {SubscriptionId} expired (AutoRenew: false)",
                        subscription.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing subscription {SubscriptionId}",
                    subscription.Id);
            }
        }

        _logger.LogInformation("Completed subscription renewal check");
    }
}
