namespace GenoDev.BusinessTracker.ApplicationLogic.Abstractions;

public interface IMailOutboxProcessor
{
    Task<bool> ProcessNextAsync(CancellationToken cancellationToken);
    Task<int> PurgeExpiredAttachmentsAsync(CancellationToken cancellationToken);
}
