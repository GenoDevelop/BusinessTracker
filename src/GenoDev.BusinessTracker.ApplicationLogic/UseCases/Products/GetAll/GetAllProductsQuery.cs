using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Products.GetAll;

public record GetAllProductsQuery(int Page, int PageSize, ProductSortBy? SortBy = null, bool IsDescending = false) : IRequest<PagedList<ProductDto>>;
