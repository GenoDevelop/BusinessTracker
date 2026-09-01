using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.Exceptions;
using GenoDev.BusinessTracker.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.SaveSmtpAccount;

public sealed class SaveSmtpAccountCommandHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<SaveSmtpAccountCommand, Guid>
{
    public async Task<Guid> Handle(SaveSmtpAccountCommand request, CancellationToken cancellationToken)
    {
        SmtpAccount account;
        if (request.Id is { } id)
        {
            account = await dbContext.SmtpAccounts.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw RequestValidationException.For("Nie znaleziono konta SMTP.", nameof(request.Id));
        }
        else
        {
            account = new SmtpAccount { Id = Guid.NewGuid() };
            dbContext.SmtpAccounts.Add(account);
        }

        if (request.IsDefault)
        {
            await dbContext.SmtpAccounts.Where(x => x.IsDefault && x.Id != account.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsDefault, false), cancellationToken);
        }

        account.Name = request.Name.Trim();
        account.Host = request.Host.Trim();
        account.Port = request.Port;
        account.UseStartTls = request.UseStartTls;
        account.UserName = request.UserName.Trim();
        if (!string.IsNullOrEmpty(request.Password)) account.Password = request.Password;
        account.FromAddress = request.FromAddress.Trim();
        account.FromName = request.FromName.Trim();
        account.ReplyToAddress = request.ReplyToAddress;
        account.IsDefault = request.IsDefault;
        account.IsEnabled = request.IsEnabled;
        await dbContext.SaveChangesAsync(cancellationToken);
        return account.Id;
    }
}
