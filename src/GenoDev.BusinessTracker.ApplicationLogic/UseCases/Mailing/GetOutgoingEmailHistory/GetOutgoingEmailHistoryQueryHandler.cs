using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.GetOutgoingEmailHistory;

public sealed class GetOutgoingEmailHistoryQueryHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<GetOutgoingEmailHistoryQuery, PagedList<OutgoingEmailListDto>>
{
    public async Task<PagedList<OutgoingEmailListDto>> Handle(GetOutgoingEmailHistoryQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.OutgoingEmails.AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id)
            .Skip(request.PageIndex * request.PageSize).Take(request.PageSize)
            .Select(x => new OutgoingEmailListDto(x.Id, x.OrderId, x.Order.OrderIdentifier, x.RecipientAddress,
                x.Subject, x.Status, x.CreatedAtUtc, x.SentAtUtc, x.Attachments.Count,
                x.Attachments.Any(a => a.Content == null), x.ErrorMessage))
            .ToListAsync(cancellationToken);
        return new PagedList<OutgoingEmailListDto>(items, totalCount, request.PageIndex, request.PageSize);
    }
}
