using HotelStay.Api.Models;

namespace HotelStay.Api.Providers;

// BudgetNests stub — snake_case source, minimal details, may return available:false
public class BudgetNestsProvider : IHotelProvider
{
    public string ProviderId => "BudgetNests";

    // Simulates snake_case JSON from provider — some rooms unavailable
    private static readonly Dictionary<string, List<BudgetRoom>> Rooms = new(StringComparer.OrdinalIgnoreCase)
    {
        ["paris"]     = [ new("Standard",  95m, "Flexible",      true),
                          new("Deluxe",   165m, "NonRefundable", true),
                          new("Suite",    310m, "Flexible",      false) ],   // unavailable
        ["london"]    = [ new("Standard", 110m, "NonRefundable", true),
                          new("Deluxe",   185m, "Flexible",      false),    // unavailable
                          new("Suite",    340m, "NonRefundable", true) ],
        ["new york"]  = [ new("Standard", 130m, "Flexible",      true),
                          new("Deluxe",   210m, "Flexible",      true),
                          new("Suite",    400m, "NonRefundable", false) ],  // unavailable
        ["dubai"]     = [ new("Standard", 100m, "NonRefundable", true),
                          new("Deluxe",   190m, "Flexible",      true),
                          new("Suite",    360m, "NonRefundable", true) ],
        ["tokyo"]     = [ new("Standard",  88m, "Flexible",      false),   // unavailable
                          new("Deluxe",   155m, "NonRefundable", true),
                          new("Suite",    290m, "Flexible",      true) ],
        ["sydney"]    = [ new("Standard",  98m, "Flexible",      true),
                          new("Deluxe",   172m, "Flexible",      true),
                          new("Suite",    320m, "NonRefundable", false) ],  // unavailable
        ["mumbai"]    = [ new("Standard",  60m, "Flexible",      true),
                          new("Deluxe",   115m, "NonRefundable", true),
                          new("Suite",    220m, "Flexible",      true) ],
        ["delhi"]     = [ new("Standard",  58m, "NonRefundable", true),
                          new("Deluxe",   108m, "Flexible",      false),   // unavailable
                          new("Suite",    200m, "NonRefundable", true) ],
        ["bangalore"] = [ new("Standard",  65m, "Flexible",      true),
                          new("Deluxe",   120m, "Flexible",      true),
                          new("Suite",    230m, "NonRefundable", false) ],  // unavailable
        ["chennai"]   = [ new("Standard",  55m, "Flexible",      true),
                          new("Deluxe",   100m, "NonRefundable", true),
                          new("Suite",    185m, "Flexible",      true) ],
        ["hyderabad"] = [ new("Standard",  60m, "NonRefundable", true),
                          new("Deluxe",   112m, "Flexible",      true),
                          new("Suite",    210m, "Flexible",      false) ],  // unavailable
    };

    public Task<IEnumerable<AvailabilityResult>> SearchAsync(SearchParams p)
    {
        var key = p.Destination.ToLower();
        if (!Rooms.TryGetValue(key, out var rooms))
            return Task.FromResult(Enumerable.Empty<AvailabilityResult>());

        var nights = p.CheckOut.DayNumber - p.CheckIn.DayNumber;

        var results = rooms
            .Where(r => r.Available)                                              // filter unavailable
            .Where(r => p.RoomType == null || r.Type == p.RoomType.ToString())
            .Select(r => new AvailabilityResult(
                Provider:           ProviderId,
                RoomType:           Enum.Parse<Models.RoomType>(r.Type),
                RatePerNight:       r.Rate,
                TotalPrice:         r.Rate * nights,
                Nights:             nights,
                CancellationPolicy: Enum.Parse<CancellationPolicy>(r.Policy),
                Amenities:          [],          // BudgetNests doesn't provide amenities
                StarRating:         null         // BudgetNests doesn't provide star rating
            ));

        return Task.FromResult(results);
    }

    private record BudgetRoom(string Type, decimal Rate, string Policy, bool Available);
}
