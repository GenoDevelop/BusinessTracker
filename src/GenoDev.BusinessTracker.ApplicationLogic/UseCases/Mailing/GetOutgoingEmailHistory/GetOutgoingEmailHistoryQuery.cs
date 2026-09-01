using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.GetOutgoingEmailHistory;

public sealed record GetOutgoingEmailHistoryQuery(int PageIndex, int PageSize) : IRequest<PagedList<OutgoingEmailListDto>>;
