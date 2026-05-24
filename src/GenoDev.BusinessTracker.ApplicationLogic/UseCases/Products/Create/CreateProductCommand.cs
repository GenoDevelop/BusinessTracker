using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Products.Create;

public record CreateProductCommand(string ProductName, string? EanCode) : IRequest<ProductDto>;
