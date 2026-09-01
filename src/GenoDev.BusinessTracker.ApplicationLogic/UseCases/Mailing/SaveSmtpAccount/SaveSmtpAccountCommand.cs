using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.SaveSmtpAccount;

public sealed record SaveSmtpAccountCommand(
    Guid? Id,
    string Name,
    string Host,
    int Port,
    bool UseStartTls,
    string UserName,
    string? Password,
    string FromAddress,
    string FromName,
    string? ReplyToAddress,
    bool IsDefault,
    bool IsEnabled) : IRequest<Guid>;
