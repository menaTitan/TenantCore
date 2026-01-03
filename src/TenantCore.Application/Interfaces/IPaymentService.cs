namespace TenantCore.Application.Interfaces;

public interface IPaymentService
{
    Task<bool> ChargeAsync(string customerId, decimal amount, string currency);
    Task<string> CreateCustomerAsync(string email, string name);
    Task<string> CreateCheckoutSessionAsync(string customerId, decimal amount, string currency);
}
