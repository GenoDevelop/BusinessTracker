using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using Microsoft.Extensions.Hosting;

namespace GenoDev.BusinessTracker.Wpf.Services;

public sealed class MailOutboxHostedService(IMailOutboxProcessor processor) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await processor.PurgeExpiredAttachmentsAsync(stoppingToken);
        var lastPurge = DateTime.UtcNow;
        while (!stoppingToken.IsCancellationRequested)
        {
            var processed = await processor.ProcessNextAsync(stoppingToken);
            if (DateTime.UtcNow - lastPurge >= TimeSpan.FromHours(1))
            {
                await processor.PurgeExpiredAttachmentsAsync(stoppingToken);
                lastPurge = DateTime.UtcNow;
            }
            if (!processed) await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
