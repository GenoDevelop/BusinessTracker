using AutoFixture;
using FluentAssertions;
using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Products.Images;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Products;

public sealed class ProductImageValidators_Tests
    : BusinessTrackerUnitTestsBase<AddProductImagesCommandValidator>
{
    protected override void RegisterMockedDependencies(
        IServiceCollection services,
        IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
        services.AddTransient<IValidator<ProductImageUpload>, ProductImageUploadValidator>();
        services.AddTransient<GetProductImagesQueryValidator>();
        services.AddTransient<GetProductImageContentQueryValidator>();
        services.AddTransient<DeleteProductImageCommandValidator>();
    }

    [Fact]
    public async Task AddValidator_ShouldRejectUnsupportedContentAndTooManyImages()
    {
        var productId = Arrange_BusinessTrackerDatabase(db => db.Arrange_Product().Id);
        var invalidImages = Enumerable.Range(0, ProductImageConstraints.MaxFilesPerUpload + 1)
            .Select(index => new ProductImageUpload($"{index}.png", "image/png", [1, 2, 3]))
            .ToArray();

        var result = await Sut.ValidateAsync(
            new AddProductImagesCommand(productId, invalidImages),
            TestContext.Current.CancellationToken);

        result.Errors.Should().Contain(x => x.PropertyName == nameof(AddProductImagesCommand.Images));
        result.Errors.Should().Contain(x => x.ErrorMessage.Contains("formatowi zdjęcia"));
    }

    [Fact]
    public async Task Validators_ShouldRejectMissingProductAndImage()
    {
        var listValidator = _sp.GetRequiredService<GetProductImagesQueryValidator>();
        var contentValidator = _sp.GetRequiredService<GetProductImageContentQueryValidator>();
        var deleteValidator = _sp.GetRequiredService<DeleteProductImageCommandValidator>();

        var listResult = await listValidator.ValidateAsync(
            new GetProductImagesQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);
        var contentResult = await contentValidator.ValidateAsync(
            new GetProductImageContentQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);
        var deleteResult = await deleteValidator.ValidateAsync(
            new DeleteProductImageCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        listResult.Errors.Should().ContainSingle(x => x.ErrorMessage == "Nie znaleziono produktu.");
        contentResult.Errors.Should().ContainSingle(x => x.ErrorMessage == "Nie znaleziono zdjęcia.");
        deleteResult.Errors.Should().ContainSingle(x => x.ErrorMessage == "Nie znaleziono zdjęcia.");
    }
}
