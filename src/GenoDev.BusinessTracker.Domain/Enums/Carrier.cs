namespace GenoDev.BusinessTracker.Domain.Enums;

public enum Carrier
{
    InPost,
    Ups
}

public static class CarrierExtensions
{
    public static string? GetTrackingUrl(this Carrier carrier, string trackingNumber)
    {
        if (string.IsNullOrWhiteSpace(trackingNumber))
            return null;

        return carrier switch
        {
            Carrier.InPost => $"https://inpost.pl/sledzenie-przesylek?number={trackingNumber}",
            _ => null
        };
    }
}
