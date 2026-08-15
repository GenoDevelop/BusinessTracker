using FluentAssertions;
using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.Extensions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.CreateOrder;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.AddProduction;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.Create;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetProducts;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.UpdateProduction;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.UpdateOrder;
using GenoDev.BusinessTracker.ApplicationLogic.Validation;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Database;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System.Runtime.CompilerServices;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.Validation;

public class ValidatorRegistration_Tests
{
    [Fact]
    public void AddApplicationServices_ShouldRegisterValidatorForEveryRequestWithProperties()
    {
        var services = new ServiceCollection();
        services.AddApplicationServices(Substitute.For<IConfiguration>());

        var applicationAssembly = typeof(DependencyInjectionExtensions).Assembly;
        var requestTypes = applicationAssembly.DefinedTypes
            .Where(type => !type.IsAbstract && type.DeclaredProperties.Any())
            .Where(type => type.ImplementedInterfaces.Any(@interface =>
                @interface == typeof(IRequest) ||
                @interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IRequest<>)))
            .Select(type => type.AsType())
            .ToArray();
        var validatedRequestTypes = services
            .Where(descriptor => descriptor.ServiceType.IsGenericType &&
                                 descriptor.ServiceType.GetGenericTypeDefinition() == typeof(IValidator<>))
            .Select(descriptor => descriptor.ServiceType.GenericTypeArguments[0])
            .ToHashSet();

        validatedRequestTypes.Should().Contain(requestTypes);
    }
}

public abstract class ValidatorContractTests<TValidator> where TValidator : class, IValidator
{
    [Fact]
    public void Validator_ShouldBeRegisteredAsTransientAndDefineAtLeastOneRule()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IBusinessTrackerDbContext>());
        services.AddApplicationServices(Substitute.For<IConfiguration>());

        var validatorInterface = typeof(TValidator).GetInterfaces().Single(@interface =>
            @interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IValidator<>));
        var registration = services.Single(descriptor =>
            descriptor.ServiceType == validatorInterface && descriptor.ImplementationType == typeof(TValidator));
        using var provider = services.BuildServiceProvider();

        var validator = provider.GetServices(validatorInterface).Cast<IValidator>().Single(item => item is TValidator);

        registration.Lifetime.Should().Be(ServiceLifetime.Transient);
        validator.CreateDescriptor().Rules.Should().NotBeEmpty();
    }
}

public sealed class AddItemToSupplyCommandValidator_Tests : ValidatorContractTests<AddItemToSupplyCommandValidator>;
public sealed class AddPackingMaterialToOrderCommandValidator_Tests : ValidatorContractTests<AddPackingMaterialToOrderCommandValidator>;
public sealed class AddProductionCommandValidator_Tests : ValidatorContractTests<AddProductionCommandValidator>;
public sealed class AddProductToOrderCommandValidator_Tests : ValidatorContractTests<AddProductToOrderCommandValidator>;
public sealed class AddRecipeMaterialCommandValidator_Tests : ValidatorContractTests<AddRecipeMaterialCommandValidator>;
public sealed class ClientDataValidator_Tests : ValidatorContractTests<ClientDataValidator>;
public sealed class CreateFixedAssetCommandValidator_Tests : ValidatorContractTests<CreateFixedAssetCommandValidator>;
public sealed class CreateMaterialCommandValidator_Tests : ValidatorContractTests<CreateMaterialCommandValidator>;
public sealed class CreateMaterialVariantCommandValidator_Tests : ValidatorContractTests<CreateMaterialVariantCommandValidator>;
public sealed class CreateOrderCommandValidator_Tests : ValidatorContractTests<CreateOrderCommandValidator>;
public sealed class CreatePackingMaterialCommandValidator_Tests : ValidatorContractTests<CreatePackingMaterialCommandValidator>;
public sealed class CreateProductCommandValidator_Tests : ValidatorContractTests<CreateProductCommandValidator>;
public sealed class CreateRecipeCommandValidator_Tests : ValidatorContractTests<CreateRecipeCommandValidator>;
public sealed class CreateSupplierCommandValidator_Tests : ValidatorContractTests<CreateSupplierCommandValidator>;
public sealed class CreateSupplyCommandValidator_Tests : ValidatorContractTests<CreateSupplyCommandValidator>;
public sealed class DeleteFixedAssetCommandValidator_Tests : ValidatorContractTests<DeleteFixedAssetCommandValidator>;
public sealed class DeleteMaterialCommandValidator_Tests : ValidatorContractTests<DeleteMaterialCommandValidator>;
public sealed class DeleteMaterialVariantCommandValidator_Tests : ValidatorContractTests<DeleteMaterialVariantCommandValidator>;
public sealed class DeleteOrderCommandValidator_Tests : ValidatorContractTests<DeleteOrderCommandValidator>;
public sealed class DeletePackingMaterialCommandValidator_Tests : ValidatorContractTests<DeletePackingMaterialCommandValidator>;
public sealed class DeletePackingMaterialFromOrderCommandValidator_Tests : ValidatorContractTests<DeletePackingMaterialFromOrderCommandValidator>;
public sealed class DeleteProductCommandValidator_Tests : ValidatorContractTests<DeleteProductCommandValidator>;
public sealed class DeleteProductFromOrderCommandValidator_Tests : ValidatorContractTests<DeleteProductFromOrderCommandValidator>;
public sealed class DeleteProductionCommandValidator_Tests : ValidatorContractTests<DeleteProductionCommandValidator>;
public sealed class DeleteRecipeCommandValidator_Tests : ValidatorContractTests<DeleteRecipeCommandValidator>;
public sealed class DeleteSupplierCommandValidator_Tests : ValidatorContractTests<DeleteSupplierCommandValidator>;
public sealed class DeleteSupplyCommandValidator_Tests : ValidatorContractTests<DeleteSupplyCommandValidator>;
public sealed class EditSupplyItemCommandValidator_Tests : ValidatorContractTests<EditSupplyItemCommandValidator>;
public sealed class GetFixedAssetsQueryValidator_Tests : ValidatorContractTests<GetFixedAssetsQueryValidator>;
public sealed class GetMaterialsForProductionQueryValidator_Tests : ValidatorContractTests<GetMaterialsForProductionQueryValidator>;
public sealed class GetMaterialsForRecipeQueryValidator_Tests : ValidatorContractTests<GetMaterialsForRecipeQueryValidator>;
public sealed class GetMaterialsQueryValidator_Tests : ValidatorContractTests<GetMaterialsQueryValidator>;
public sealed class GetMaterialVariantsForProductionQueryValidator_Tests : ValidatorContractTests<GetMaterialVariantsForProductionQueryValidator>;
public sealed class GetMaterialVariantsQueryValidator_Tests : ValidatorContractTests<GetMaterialVariantsQueryValidator>;
public sealed class GetOrderPackingMaterialsQueryValidator_Tests : ValidatorContractTests<GetOrderPackingMaterialsQueryValidator>;
public sealed class GetOrderProductsQueryValidator_Tests : ValidatorContractTests<GetOrderProductsQueryValidator>;
public sealed class GetOrdersQueryValidator_Tests : ValidatorContractTests<GetOrdersQueryValidator>;
public sealed class GetPackingMaterialsQueryValidator_Tests : ValidatorContractTests<GetPackingMaterialsQueryValidator>;
public sealed class GetProductionHistoryQueryValidator_Tests : ValidatorContractTests<GetProductionHistoryQueryValidator>;
public sealed class GetProductionMaterialsQueryValidator_Tests : ValidatorContractTests<GetProductionMaterialsQueryValidator>;
public sealed class GetProductionSummaryQueryValidator_Tests : ValidatorContractTests<GetProductionSummaryQueryValidator>;
public sealed class GetProductsQueryValidator_Tests : ValidatorContractTests<GetProductsQueryValidator>;
public sealed class GetRecipeMaterialsQueryValidator_Tests : ValidatorContractTests<GetRecipeMaterialsQueryValidator>;
public sealed class GetRecipesQueryValidator_Tests : ValidatorContractTests<GetRecipesQueryValidator>;
public sealed class GetSuppliersQueryValidator_Tests : ValidatorContractTests<GetSuppliersQueryValidator>;
public sealed class GetSuppliesQueryValidator_Tests : ValidatorContractTests<GetSuppliesQueryValidator>;
public sealed class GetSupplyDetailsQueryValidator_Tests : ValidatorContractTests<GetSupplyDetailsQueryValidator>;
public sealed class GetSupplyItemsQueryValidator_Tests : ValidatorContractTests<GetSupplyItemsQueryValidator>;
public sealed class MaterialVariantUsageDtoValidator_Tests : ValidatorContractTests<AddProductionCommandValidator.MaterialVariantUsageDtoValidator>;
public sealed class OrderDataValidator_Tests : ValidatorContractTests<OrderDataValidator>;
public sealed class RemoveItemFromSupplyCommandValidator_Tests : ValidatorContractTests<RemoveItemFromSupplyCommandValidator>;
public sealed class RemoveRecipeMaterialCommandValidator_Tests : ValidatorContractTests<RemoveRecipeMaterialCommandValidator>;
public sealed class UpdateClientDataValidator_Tests : ValidatorContractTests<UpdateClientDataValidator>;
public sealed class UpdateFixedAssetCommandValidator_Tests : ValidatorContractTests<UpdateFixedAssetCommandValidator>;
public sealed class UpdateMaterialCommandValidator_Tests : ValidatorContractTests<UpdateMaterialCommandValidator>;
public sealed class UpdateMaterialVariantCommandValidator_Tests : ValidatorContractTests<UpdateMaterialVariantCommandValidator>;
public sealed class UpdateOrderCommandValidator_Tests : ValidatorContractTests<UpdateOrderCommandValidator>;
public sealed class UpdateOrderDataValidator_Tests : ValidatorContractTests<UpdateOrderDataValidator>;
public sealed class UpdateOrderPackingMaterialCommandValidator_Tests : ValidatorContractTests<UpdateOrderPackingMaterialCommandValidator>;
public sealed class UpdateOrderProductCommandValidator_Tests : ValidatorContractTests<UpdateOrderProductCommandValidator>;
public sealed class UpdatePackingMaterialCommandValidator_Tests : ValidatorContractTests<UpdatePackingMaterialCommandValidator>;
public sealed class UpdateProductCommandValidator_Tests : ValidatorContractTests<UpdateProductCommandValidator>;
public sealed class UpdateProductionCommandValidator_Tests : ValidatorContractTests<UpdateProductionCommandValidator>;
public sealed class UpdateRecipeCommandValidator_Tests : ValidatorContractTests<UpdateRecipeCommandValidator>;
public sealed class UpdateRecipeMaterialCommandValidator_Tests : ValidatorContractTests<UpdateRecipeMaterialCommandValidator>;
public sealed class UpdateSupplierCommandValidator_Tests : ValidatorContractTests<UpdateSupplierCommandValidator>;
public sealed class UpdateSupplyCommandValidator_Tests : ValidatorContractTests<UpdateSupplyCommandValidator>;

public class IndependentSubValidatorsBehavior_Tests
{
    private static readonly string TooLongName = new('x', 201);
    private static readonly string TooLongDescription = new('x', 4001);

    [Fact]
    public async Task OrderDataValidator_ShouldValidateEveryOwnedField()
    {
        var validator = new OrderDataValidator();
        var data = new OrderData(TooLongDescription, default, TooLongName, TooLongName, TooLongName,
            (Carrier)int.MaxValue, false, TooLongName, -1, -2, -1, -2);

        var result = await validator.ValidateAsync(data, TestContext.Current.CancellationToken);

        result.Errors.Select(error => error.PropertyName).Should().Contain([
            nameof(OrderData.Description), nameof(OrderData.OrderDate), nameof(OrderData.OrderIdentifier),
            nameof(OrderData.PaymentIdentifier), nameof(OrderData.TrackingNumber), nameof(OrderData.Carrier),
            nameof(OrderData.OrderSource), nameof(OrderData.ShippingNetCost), nameof(OrderData.ShippingGrossCost),
            nameof(OrderData.ShippingNetClientPrice), nameof(OrderData.ShippingGrossClientPrice)
        ]);
    }

    [Fact]
    public async Task ClientDataValidator_ShouldValidateEveryOwnedField()
    {
        var validator = new ClientDataValidator();
        var data = new ClientData(TooLongName, TooLongName, TooLongName, TooLongName, "niepoprawny-email",
            TooLongName, TooLongDescription);

        var result = await validator.ValidateAsync(data, TestContext.Current.CancellationToken);

        result.Errors.Select(error => error.PropertyName).Should().Contain([
            nameof(ClientData.ClientName), nameof(ClientData.Street), nameof(ClientData.PostCode),
            nameof(ClientData.City), nameof(ClientData.Email), nameof(ClientData.Phone),
            nameof(ClientData.ClientDescription)
        ]);
    }

    [Fact]
    public async Task UpdateOrderDataValidator_ShouldValidateEveryOwnedField()
    {
        var validator = new UpdateOrderDataValidator();
        var data = new UpdateOrderData(TooLongDescription, default, TooLongName, TooLongName, TooLongName,
            (Carrier)int.MaxValue, (OrderStatus)int.MaxValue, false, TooLongName, -1, -2, -1, -2);

        var result = await validator.ValidateAsync(data, TestContext.Current.CancellationToken);

        result.Errors.Select(error => error.PropertyName).Should().Contain([
            nameof(UpdateOrderData.Description), nameof(UpdateOrderData.OrderDate), nameof(UpdateOrderData.OrderIdentifier),
            nameof(UpdateOrderData.PaymentIdentifier), nameof(UpdateOrderData.TrackingNumber), nameof(UpdateOrderData.Carrier),
            nameof(UpdateOrderData.Status), nameof(UpdateOrderData.OrderSource), nameof(UpdateOrderData.ShippingNetCost),
            nameof(UpdateOrderData.ShippingGrossCost), nameof(UpdateOrderData.ShippingNetClientPrice),
            nameof(UpdateOrderData.ShippingGrossClientPrice)
        ]);
    }

    [Fact]
    public async Task UpdateClientDataValidator_ShouldValidateEveryOwnedField()
    {
        var validator = new UpdateClientDataValidator();
        var data = new UpdateClientData(TooLongName, TooLongName, TooLongName, TooLongName, "niepoprawny-email",
            TooLongName, TooLongDescription);

        var result = await validator.ValidateAsync(data, TestContext.Current.CancellationToken);

        result.Errors.Select(error => error.PropertyName).Should().Contain([
            nameof(UpdateClientData.ClientName), nameof(UpdateClientData.Street), nameof(UpdateClientData.PostCode),
            nameof(UpdateClientData.City), nameof(UpdateClientData.Email), nameof(UpdateClientData.Phone),
            nameof(UpdateClientData.ClientDescription)
        ]);
    }
}

public class DatabaseValidatorsBehavior_Tests : BusinessTrackerUnitTestsBase<CreateProductCommandValidator>
{
    public static IEnumerable<object[]> AllValidatorTypes =>
        typeof(DependencyInjectionExtensions).Assembly.DefinedTypes
            .Where(type => !type.IsAbstract && type.ImplementedInterfaces.Any(@interface =>
                @interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IValidator<>)))
            .OrderBy(type => type.FullName)
            .Select(type => new object[] { type.AsType() });

    protected override void RegisterMockedDependencies(IServiceCollection services, AutoFixture.IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
        services.AddApplicationServices(Substitute.For<IConfiguration>());
    }

    [Theory]
    [MemberData(nameof(AllValidatorTypes))]
    public async Task Validator_ShouldRejectInvalidRequestWithPolishMessages(Type validatorType)
    {
        var validatorInterface = validatorType.GetInterfaces().Single(@interface =>
            @interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IValidator<>));
        var requestType = validatorInterface.GenericTypeArguments[0];
        var validator = _sp.GetServices(validatorInterface).Cast<IValidator>()
            .Single(item => item.GetType() == validatorType);
        var request = CreateInvalidRequest(requestType);
        var contextType = typeof(ValidationContext<>).MakeGenericType(requestType);
        var context = (IValidationContext)Activator.CreateInstance(contextType, request)!;

        var result = await validator.ValidateAsync(context, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse($"{validatorType.Name} powinien odrzucić niepoprawny request");
        result.Errors.Should().OnlyContain(error =>
            !string.IsNullOrWhiteSpace(error.ErrorMessage) &&
            !error.ErrorMessage.Contains("must", StringComparison.OrdinalIgnoreCase) &&
            !error.ErrorMessage.Contains("invalid", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CreateProductValidator_ShouldReturnRequiredAndUniqueIdentifierErrorsInPolish()
    {
        Arrange_BusinessTrackerDatabase(db => db.Arrange_Product(identifier: "SKU-1"));
        var validator = _sp.GetRequiredService<IValidator<CreateProductCommand>>();

        var emptyResult = await validator.ValidateAsync(
            new CreateProductCommand(string.Empty, string.Empty, null), TestContext.Current.CancellationToken);
        var duplicateResult = await validator.ValidateAsync(
            new CreateProductCommand("Produkt", "SKU-1", null), TestContext.Current.CancellationToken);

        emptyResult.Errors.Should().Contain(error =>
            error.PropertyName == nameof(CreateProductCommand.Name) && error.ErrorMessage.Contains("wymagane"));
        duplicateResult.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(CreateProductCommand.Identifier) &&
            error.ErrorMessage == "Produkt o podanym identyfikatorze już istnieje.");
    }

    [Fact]
    public async Task CreateOrderValidator_ShouldOnlyValidatePresenceOfItsChildObjects()
    {
        var validator = _sp.GetRequiredService<IValidator<CreateOrderCommand>>();

        var missingOrder = await validator.ValidateAsync(
            new CreateOrderCommand(null!, ValidClientData()), TestContext.Current.CancellationToken);
        var missingClient = await validator.ValidateAsync(
            new CreateOrderCommand(ValidOrderData(), null!), TestContext.Current.CancellationToken);

        missingOrder.Errors.Should().ContainSingle(error => error.PropertyName == nameof(CreateOrderCommand.Order));
        missingClient.Errors.Should().ContainSingle(error => error.PropertyName == nameof(CreateOrderCommand.Client));
    }

    [Fact]
    public async Task UpdateOrderValidator_ShouldOnlyValidateExistenceAndPresenceOfItsChildObjects()
    {
        var orderId = Arrange_BusinessTrackerDatabase(db => db.Arrange_Order().Id);
        var validator = _sp.GetRequiredService<IValidator<UpdateOrderCommand>>();

        var missingOrder = await validator.ValidateAsync(
            new UpdateOrderCommand(orderId, null!, ValidUpdateClientData()), TestContext.Current.CancellationToken);
        var missingClient = await validator.ValidateAsync(
            new UpdateOrderCommand(orderId, ValidUpdateOrderData(), null!), TestContext.Current.CancellationToken);

        missingOrder.Errors.Should().ContainSingle(error => error.PropertyName == nameof(UpdateOrderCommand.Order));
        missingClient.Errors.Should().ContainSingle(error => error.PropertyName == nameof(UpdateOrderCommand.Client));
    }

    [Fact]
    public async Task MaterialVariantUsageValidator_ShouldValidateItsOwnFieldsIndependently()
    {
        var validator = _sp.GetRequiredService<IValidator<MaterialVariantUsageDto>>();

        var result = await validator.ValidateAsync(
            new MaterialVariantUsageDto(null, Guid.Empty, 0), TestContext.Current.CancellationToken);

        result.Errors.Should().Contain(error => error.PropertyName == nameof(MaterialVariantUsageDto.MaterialVariantId));
        result.Errors.Should().Contain(error => error.PropertyName == nameof(MaterialVariantUsageDto.Amount));
    }

    [Fact]
    public async Task AddProductionValidator_ShouldValidateOnlyParentRulesWhenChildValidatorAcceptsEverything()
    {
        var db = _sp.GetRequiredService<IBusinessTrackerDbContext>();
        var validator = new AddProductionCommandValidator(db, new InlineValidator<MaterialVariantUsageDto>());
        var variantId = Guid.NewGuid();
        var command = new AddProductionCommand(Guid.Empty, 0, new string('x', 4001), default,
            [new MaterialVariantUsageDto(null, variantId, 0), new MaterialVariantUsageDto(null, variantId, 0)]);

        var result = await validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.Errors.Select(error => error.PropertyName).Should().Contain([
            nameof(AddProductionCommand.ProductId), nameof(AddProductionCommand.Amount),
            nameof(AddProductionCommand.Description), nameof(AddProductionCommand.ProductionDate),
            nameof(AddProductionCommand.UsedMaterials)
        ]);
        result.Errors.Should().NotContain(error => error.PropertyName.StartsWith(
            $"{nameof(AddProductionCommand.UsedMaterials)}[", StringComparison.Ordinal));
    }

    [Fact]
    public async Task UpdateProductionValidator_ShouldValidateOnlyParentRulesWhenChildValidatorAcceptsEverything()
    {
        var db = _sp.GetRequiredService<IBusinessTrackerDbContext>();
        var validator = new UpdateProductionCommandValidator(db, new InlineValidator<MaterialVariantUsageDto>());
        var variantId = Guid.NewGuid();
        var command = new UpdateProductionCommand(Guid.Empty, 0, new string('x', 4001), default,
            [new MaterialVariantUsageDto(null, variantId, 0), new MaterialVariantUsageDto(null, variantId, 0)]);

        var result = await validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.Errors.Select(error => error.PropertyName).Should().Contain([
            nameof(UpdateProductionCommand.Id), nameof(UpdateProductionCommand.Amount),
            nameof(UpdateProductionCommand.Description), nameof(UpdateProductionCommand.ProductionDate),
            nameof(UpdateProductionCommand.UsedMaterials)
        ]);
        result.Errors.Should().NotContain(error => error.PropertyName.StartsWith(
            $"{nameof(UpdateProductionCommand.UsedMaterials)}[", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PagedQueryValidator_ShouldRejectInvalidPageParameters()
    {
        var validator = _sp.GetRequiredService<IValidator<GetProductsQuery>>();

        var result = await validator.ValidateAsync(
            new GetProductsQuery(-1, 1001), TestContext.Current.CancellationToken);

        result.Errors.Should().Contain(error => error.PropertyName == nameof(GetProductsQuery.PageIndex));
        result.Errors.Should().Contain(error => error.PropertyName == nameof(GetProductsQuery.PageSize));
    }

    private static OrderData ValidOrderData() =>
        new(null, DateTime.Today, null, null, null, null, false, "Sklep", 0, 0, 0, 0);

    private static ClientData ValidClientData() => new(null, null, null, null, null, null, null);

    private static UpdateOrderData ValidUpdateOrderData() =>
        new(null, DateTime.Today, null, null, null, null, OrderStatus.New, false, "Sklep", 0, 0, 0, 0);

    private static UpdateClientData ValidUpdateClientData() => new(null, null, null, null, null, null, null);

    private static object CreateInvalidRequest(Type requestType)
    {
        var request = RuntimeHelpers.GetUninitializedObject(requestType);

        foreach (var property in requestType.GetProperties().Where(property => property.SetMethod is not null))
        {
            var value = CreateInvalidValue(property.PropertyType);
            if (value is not null || !property.PropertyType.IsValueType || Nullable.GetUnderlyingType(property.PropertyType) is not null)
                property.SetValue(request, value);
        }

        return request;
    }

    private static object? CreateInvalidValue(Type propertyType)
    {
        var effectiveType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (effectiveType == typeof(string)) return new string('x', 4001);
        if (effectiveType == typeof(Guid)) return Guid.Empty;
        if (effectiveType == typeof(DateTime)) return default(DateTime);
        if (effectiveType == typeof(int)) return -1;
        if (effectiveType == typeof(double)) return -1d;
        if (effectiveType == typeof(decimal)) return -1m;
        if (effectiveType.IsEnum) return Enum.ToObject(effectiveType, int.MaxValue);

        if (propertyType.IsArray)
        {
            var elementType = propertyType.GetElementType()!;
            var array = Array.CreateInstance(elementType, 1);
            array.SetValue(CreateInvalidValue(elementType), 0);
            return array;
        }

        var enumerableInterface = propertyType.IsGenericType &&
                                  propertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            ? propertyType
            : propertyType.GetInterfaces().FirstOrDefault(@interface =>
                @interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        if (enumerableInterface is not null)
        {
            var elementType = enumerableInterface.GenericTypeArguments[0];
            var array = Array.CreateInstance(elementType, 1);
            var element = CreateInvalidValue(elementType) ??
                          (elementType.IsClass ? CreateInvalidRequest(elementType) : null);
            array.SetValue(element, 0);
            return array;
        }

        return null;
    }
}
