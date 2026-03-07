using ThinkBridge_ERP.Data;
using ThinkBridge_ERP.Services.Interfaces;

namespace ThinkBridge_ERP.Services;

/// <summary>
/// Background service that checks every hour for expired subscriptions
/// and suspends the corresponding companies.
/// </summary>
public class SubscriptionExpirationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SubscriptionExpirationService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

    public SubscriptionExpirationService(
        IServiceProvider serviceProvider,
        ILogger<SubscriptionExpirationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Subscription Expiration Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();

                var expiredCount = await subscriptionService.ExpireOverdueSubscriptionsAsync();

                if (expiredCount > 0)
                {
                    _logger.LogInformation("{Count} subscription(s) expired and companies suspended.", expiredCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in subscription expiration check.");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }
}
