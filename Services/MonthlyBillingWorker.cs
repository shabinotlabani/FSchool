namespace _2Korriku.Services;

public class MonthlyBillingWorker(IServiceScopeFactory scopes, ILogger<MonthlyBillingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                foreach (var clubId in await ClubJobs.ActiveIds(scopes, stoppingToken))
                {
                    try
                    {
                        using var scope = scopes.CreateScope();
                        ClubJobs.SetClub(scope.ServiceProvider, clubId);
                        var count = await scope.ServiceProvider.GetRequiredService<BillingService>().GenerateDueFeesAsync(ct: stoppingToken);
                        if (count > 0) logger.LogInformation("Club {ClubId}: generated {Count} monthly invoices.", clubId, count);
                    }
                    catch (Exception ex) when (!stoppingToken.IsCancellationRequested) { logger.LogError(ex, "Club {ClubId}: monthly billing deferred.", clubId); }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Monthly billing failed; it will retry automatically."); }
            try { await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
