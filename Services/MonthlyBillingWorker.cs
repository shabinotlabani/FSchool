namespace _2Korriku.Services;

public class MonthlyBillingWorker(IServiceScopeFactory scopes, ILogger<MonthlyBillingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var count = await scope.ServiceProvider.GetRequiredService<BillingService>().GenerateDueFeesAsync(ct: stoppingToken);
                if (count > 0) logger.LogInformation("Generated {Count} monthly invoices.", count);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Monthly billing failed; it will retry automatically."); }
            try { await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
