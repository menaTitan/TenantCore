using Microsoft.Extensions.Logging;
using TenantCore.Application.Interfaces;

namespace TenantCore.Infrastructure.Services;

public class StripePaymentService : IPaymentService
{
    private readonly ILogger<StripePaymentService> _logger;

    public StripePaymentService(ILogger<StripePaymentService> logger)
    {
        _logger = logger;
    }

    public Task<bool> ChargeAsync(string customerId, decimal amount, string currency)
    {
        _logger.LogInformation("Mock Stripe Charge: Customer={CustomerId}, Amount={Amount} {Currency}",
            customerId, amount, currency);
        return Task.FromResult(true);
    }

    public Task<string> CreateCustomerAsync(string email, string name)
    {
        var mockCustomerId = $"cus_mock_{Guid.NewGuid():N}";
        _logger.LogInformation("Mock Stripe Create Customer: Email={Email}, Name={Name}, CustomerId={CustomerId}",
            email, name, mockCustomerId);
        return Task.FromResult(mockCustomerId);
    }

    public Task<string> CreateCheckoutSessionAsync(string customerId, decimal amount, string currency)
    {
        var mockSessionId = $"cs_mock_{Guid.NewGuid():N}";
        _logger.LogInformation("Mock Stripe Checkout Session: Customer={CustomerId}, Amount={Amount} {Currency}",
            customerId, amount, currency);
        return Task.FromResult(mockSessionId);
    }
}
