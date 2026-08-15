using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Notes.UpdateContent;

public sealed record UpdateNoteContentCommand(
    Guid Id,
    string ContentRtf) : IRequest;
