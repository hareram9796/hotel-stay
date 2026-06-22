using HotelStay.Api.Models;
using HotelStay.Api.Providers;

namespace HotelStay.Api.Services;

public class HotelService(IEnumerable<IHotelProvider> providers)
{
    // Cities classification
    private static readonly HashSet<string> DomesticCities = new(StringComparer.OrdinalIgnoreCase)
        { "Mumbai", "Delhi", "Bangalore", "Chennai", "Hyderabad" };

    private static readonly HashSet<string> InternationalCities = new(StringComparer.OrdinalIgnoreCase)
        { "Paris", "London", "New York", "Dubai", "Tokyo", "Sydney" };

    // In-memory reservation store (no persistence required)
    private static readonly Dictionary<string, ReservationResponse> Reservations = [];

    public bool IsKnownDestination(string destination) =>
        DomesticCities.Contains(destination) || InternationalCities.Contains(destination);

    public bool IsDomestic(string destination) => DomesticCities.Contains(destination);
    public bool IsInternational(string destination) => InternationalCities.Contains(destination);

    public async Task<IEnumerable<AvailabilityResult>> SearchAsync(SearchParams p)
    {
        // Query all providers in parallel
        var tasks = providers.Select(prov => prov.SearchAsync(p));
        var results = await Task.WhenAll(tasks);
        return results.SelectMany(r => r);
    }

    public ValidationResult ValidateDocument(string destination, DocumentType docType)
    {
        if (IsInternational(destination) && docType != DocumentType.Passport)
            return ValidationResult.Fail("International destination requires a Passport.");

        if (IsDomestic(destination) && docType == DocumentType.NationalId)
            return ValidationResult.Ok();

        if (IsDomestic(destination) && docType == DocumentType.Passport)
            return ValidationResult.Ok(); // Passport also accepted for domestic

        return ValidationResult.Ok();
    }

    public ReservationResponse CreateReservation(ReservationRequest req, AvailabilityResult room)
    {
        var reference = $"SKY-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        var reservation = new ReservationResponse(
            ReferenceNumber:    reference,
            Provider:           req.ProviderId,
            Destination:        req.Destination,
            CheckIn:            req.CheckIn,
            CheckOut:           req.CheckOut,
            RoomType:           req.RoomType,
            TotalPrice:         room.TotalPrice,
            CancellationPolicy: room.CancellationPolicy,
            GuestName:          req.GuestName
        );
        Reservations[reference] = reservation;
        return reservation;
    }

    public ReservationResponse? GetReservation(string reference) =>
        Reservations.TryGetValue(reference, out var r) ? r : null;

    public IHotelProvider? GetProvider(string providerId) =>
        providers.FirstOrDefault(p => p.ProviderId == providerId);
}

public record ValidationResult(bool IsValid, string? Message)
{
    public static ValidationResult Ok() => new(true, null);
    public static ValidationResult Fail(string msg) => new(false, msg);
}
