namespace ThinkBridge_ERP.Services.Interfaces;

public interface IPayMongoService
{
    Task<PayMongoCheckoutResult> CreateCheckoutSessionAsync(PayMongoCheckoutRequest request);
    Task<PayMongoWebhookResult> ProcessWebhookAsync(string payload, string signatureHeader);
}

public class PayMongoCheckoutRequest
{
    public int SubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    /// <summary>
    /// Base URL of the current request (e.g. "https://yourdomain.com"). Used to build payment redirect URLs.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;
}

public class PayMongoCheckoutResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? CheckoutUrl { get; set; }
    public string? CheckoutSessionId { get; set; }
}

public class PayMongoWebhookResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? CheckoutSessionId { get; set; }
    public string? EventType { get; set; }
    public string? PaymentId { get; set; }
}
