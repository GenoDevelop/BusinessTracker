using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Products.Update;

public record UpdateProductCommand(Guid Id, string ProductName, string? EanCode) : IRequest<ProductDto>;
