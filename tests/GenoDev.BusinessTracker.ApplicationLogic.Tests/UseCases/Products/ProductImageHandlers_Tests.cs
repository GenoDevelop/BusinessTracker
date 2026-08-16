using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.Exceptions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Products.Images;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Products;

public sealed class ProductImageHandlers_Tests
    : BusinessTrackerUnitTestsBase<AddProductImagesCommandHandler>
{
    protected override void RegisterMockedDependencies(
        IServiceCollection services,
        IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
        services.AddTransient<GetProductImagesQueryHandler>();
        services.AddTransient<GetProductImageContentQueryHandler>();
        services.AddTransient<DeleteProductImageCommandHandler>();
    }

    [Fact]
    public async Task Add_ShouldPersistEveryImageAndReturnIds()
    {
        var productId = Arrange_BusinessTrackerDatabase(db => db.Arrange_Product().Id);
        var command = new AddProductImagesCommand(productId,
        [
            new ProductImageUpload("front.png", "image/png", ValidPngContent(1)),
            new ProductImageUpload("back.jpg", "image/jpeg", ValidJpegContent(2))
        ]);

        var result = await Sut.Handle(command, TestContext.Current.CancellationToken);

        result.Should().HaveCount(2).And.OnlyHaveUniqueItems();
        Assert_BusinessTrackerDatabase(db =>
        {
            var images = db.ProductImages.Where(x => x.ProductId == productId).ToArray();
            images.Should().HaveCount(2);
            images.Select(x => x.Id).Should().BeEquivalentTo(result);
            images.Select(x => x.FileName).Should().BeEquivalentTo("front.png", "back.jpg");
            images.Should().OnlyContain(x => x.CreatedAtUtc.Kind == DateTimeKind.Utc);
        });
    }

    [Fact]
    public async Task Add_ShouldRejectProductRemovedAfterValidation()
    {
        var act = () => Sut.Handle(
            new AddProductImagesCommand(Guid.NewGuid(),
                [new ProductImageUpload("front.png", "image/png", ValidPngContent())]),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<RequestValidationException>();
    }

    [Fact]
    public async Task List_ShouldReturnOnlyRequestedProductMetadata_InOldestFirstOrder()
    {
        var productId = Arrange_BusinessTrackerDatabase(db =>
        {
            var product = db.Arrange_Product();
            db.Arrange_ProductImage(product, fileName: "older.png", content: ValidPngContent(1),
                createdAtUtc: new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc));
            db.Arrange_ProductImage(product, fileName: "newer.png", content: ValidPngContent(2),
                createdAtUtc: new DateTime(2026, 1, 2, 10, 0, 0, DateTimeKind.Utc));
            db.Arrange_ProductImage(fileName: "other.png", content: ValidPngContent(3));
            return product.Id;
        });
        var handler = _sp.GetRequiredService<GetProductImagesQueryHandler>();

        var result = await handler.Handle(
            new GetProductImagesQuery(productId),
            TestContext.Current.CancellationToken);

        result.Select(x => x.FileName).Should().Equal("older.png", "newer.png");
        result.Should().OnlyContain(x => x.ContentType == "image/png");
    }

    [Fact]
    public async Task Content_ShouldReturnExactStoredBytes()
    {
        var expectedContent = ValidPngContent(7);
        var imageId = Arrange_BusinessTrackerDatabase(db =>
            db.Arrange_ProductImage(content: expectedContent).Id);
        var handler = _sp.GetRequiredService<GetProductImageContentQueryHandler>();

        var result = await handler.Handle(
            new GetProductImageContentQuery(imageId),
            TestContext.Current.CancellationToken);

        result.Id.Should().Be(imageId);
        result.ContentType.Should().Be("image/png");
        result.Content.Should().Equal(expectedContent);
    }

    [Fact]
    public async Task Delete_ShouldRemoveOnlySelectedImage()
    {
        var ids = Arrange_BusinessTrackerDatabase(db =>
        {
            var product = db.Arrange_Product();
            return (
                Deleted: db.Arrange_ProductImage(product, fileName: "delete.png").Id,
                Preserved: db.Arrange_ProductImage(product, fileName: "keep.png").Id);
        });
        var handler = _sp.GetRequiredService<DeleteProductImageCommandHandler>();

        await handler.Handle(
            new DeleteProductImageCommand(ids.Deleted),
            TestContext.Current.CancellationToken);

        Assert_BusinessTrackerDatabase(db =>
        {
            db.ProductImages.Any(x => x.Id == ids.Deleted).Should().BeFalse();
            db.ProductImages.Any(x => x.Id == ids.Preserved).Should().BeTrue();
        });
    }

    [Fact]
    public async Task Delete_ShouldRejectImageRemovedAfterValidation()
    {
        var handler = _sp.GetRequiredService<DeleteProductImageCommandHandler>();

        var act = () => handler.Handle(
            new DeleteProductImageCommand(Guid.NewGuid()),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<RequestValidationException>();
    }

    private static byte[] ValidPngContent(byte suffix = 0) =>
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, suffix];

    private static byte[] ValidJpegContent(byte suffix = 0) => [0xFF, 0xD8, 0xFF, suffix];
}
