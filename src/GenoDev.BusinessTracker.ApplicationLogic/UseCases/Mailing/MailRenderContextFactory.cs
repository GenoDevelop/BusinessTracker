using System.Globalization;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing;

internal static class MailRenderContextFactory
{
    private static readonly CultureInfo PolishCulture = CultureInfo.GetCultureInfo("pl-PL");

    public static MailRenderContext Create(Order order, SmtpAccount? account)
    {
        var client = order.ClientDetails;
        var totalNet = order.OrderProducts.Sum(x => x.UnitNetPrice * x.OrderedAmount) + order.ShippingNetClientPrice;
        var totalGross = order.OrderProducts.Sum(x => x.UnitGrossPrice * x.OrderedAmount) + order.ShippingGrossClientPrice;
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["order.id"] = order.Id.ToString(),
            ["order.identifier"] = order.OrderIdentifier,
            ["order.paymentIdentifier"] = order.PaymentIdentifier,
            ["order.orderDate"] = order.OrderDate.ToString("dd.MM.yyyy", PolishCulture),
            ["order.status"] = order.Status.ToString(),
            ["order.source"] = order.OrderSource,
            ["order.description"] = order.Description,
            ["order.trackingNumber"] = order.TrackingNumber,
            ["order.trackingUrl"] = order.Carrier?.GetTrackingUrl(order.TrackingNumber ?? string.Empty),
            ["order.carrier"] = order.Carrier?.ToString(),
            ["order.totalNetPrice"] = totalNet.ToString("N2", PolishCulture) + " zł",
            ["order.totalGrossPrice"] = totalGross.ToString("N2", PolishCulture) + " zł",
            ["order.shippingNetClientPrice"] = order.ShippingNetClientPrice.ToString("N2", PolishCulture) + " zł",
            ["order.shippingGrossClientPrice"] = order.ShippingGrossClientPrice.ToString("N2", PolishCulture) + " zł",
            ["client.name"] = client?.ClientName,
            ["client.email"] = client?.Email,
            ["client.phone"] = client?.Phone,
            ["client.street"] = client?.Street,
            ["client.postCode"] = client?.PostCode,
            ["client.city"] = client?.City,
            ["client.description"] = client?.Description,
            ["client.isCompany"] = order.CompanyOrder.ToString(),
            ["client.isNotCompany"] = (!order.CompanyOrder).ToString(),
            ["sender.name"] = account?.FromName,
            ["sender.email"] = account?.FromAddress
        };

        var products = order.OrderProducts.OrderBy(x => x.Product.Name).Select(x => new MailRenderItem(
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["product.name"] = x.Product.Name,
                ["product.identifier"] = x.Product.Identifier,
                ["product.orderedAmount"] = x.OrderedAmount.ToString(PolishCulture),
                ["product.assignedAmount"] = x.AssignedAmount.ToString(PolishCulture),
                ["product.unitNetPrice"] = x.UnitNetPrice.ToString("N2", PolishCulture) + " zł",
                ["product.unitGrossPrice"] = x.UnitGrossPrice.ToString("N2", PolishCulture) + " zł",
                ["product.totalNetPrice"] = (x.UnitNetPrice * x.OrderedAmount).ToString("N2", PolishCulture) + " zł",
                ["product.totalGrossPrice"] = (x.UnitGrossPrice * x.OrderedAmount).ToString("N2", PolishCulture) + " zł"
            })).ToList();

        var packingMaterials = order.OrderPackingMaterials.OrderBy(x => x.PackingMaterial.Name).Select(x => new MailRenderItem(
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["packingMaterial.name"] = x.PackingMaterial.Name,
                ["packingMaterial.amount"] = x.Amount.ToString("N2", PolishCulture),
                ["packingMaterial.unit"] = x.PackingMaterial.Unit
            })).ToList();

        return new MailRenderContext(values, new Dictionary<string, IReadOnlyList<MailRenderItem>>(StringComparer.OrdinalIgnoreCase)
        {
            ["order.products"] = products,
            ["order.packingMaterials"] = packingMaterials
        });
    }
}
