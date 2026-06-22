using HotelStay.Api.Models;

namespace HotelStay.Api.Providers;

// PremierStays stub — PascalCase, always available, full details
public class PremierStaysProvider : IHotelProvider
{
    public string ProviderId => "PremierStays";

    // Deterministic stub data keyed by destination
    private static readonly Dictionary<string, List<StubRoom>> Rooms = new(StringComparer.OrdinalIgnoreCase)
    {
        ["paris"]     = [ new("Standard", 120m, "FreeCancellation", ["WiFi","Breakfast"], 4),
                          new("Deluxe",   200m, "FreeCancellation", ["WiFi","Breakfast","Pool"], 5),
                          new("Suite",    380m, "NonRefundable",    ["WiFi","Breakfast","Pool","Spa"], 5) ],
        ["london"]    = [ new("Standard", 140m, "FreeCancellation", ["WiFi"], 3),
                          new("Deluxe",   220m, "NonRefundable",    ["WiFi","Gym"], 4),
                          new("Suite",    420m, "FreeCancellation", ["WiFi","Gym","Spa"], 5) ],
        ["new york"]  = [ new("Standard", 160m, "NonRefundable",    ["WiFi"], 3),
                          new("Deluxe",   260m, "FreeCancellation", ["WiFi","Breakfast"], 4),
                          new("Suite",    500m, "FreeCancellation", ["WiFi","Breakfast","Pool"], 5) ],
        ["dubai"]     = [ new("Standard", 130m, "FreeCancellation", ["WiFi","Pool"], 4),
                          new("Deluxe",   240m, "FreeCancellation", ["WiFi","Pool","Gym"], 5),
                          new("Suite",    460m, "NonRefundable",    ["WiFi","Pool","Gym","Spa"], 5) ],
        ["tokyo"]     = [ new("Standard", 110m, "FreeCancellation", ["WiFi"], 3),
                          new("Deluxe",   190m, "NonRefundable",    ["WiFi","Breakfast"], 4),
                          new("Suite",    350m, "FreeCancellation", ["WiFi","Breakfast","Spa"], 5) ],
        ["sydney"]    = [ new("Standard", 125m, "FreeCancellation", ["WiFi","Breakfast"], 4),
                          new("Deluxe",   210m, "FreeCancellation", ["WiFi","Breakfast","Pool"], 4),
                          new("Suite",    390m, "NonRefundable",    ["WiFi","Pool","Spa"], 5) ],
        ["mumbai"]    = [ new("Standard",  80m, "FreeCancellation", ["WiFi"], 3),
                          new("Deluxe",   150m, "FreeCancellation", ["WiFi","Breakfast"], 4),
                          new("Suite",    280m, "NonRefundable",    ["WiFi","Breakfast","Pool"], 5) ],
        ["delhi"]     = [ new("Standard",  75m, "NonRefundable",    ["WiFi"], 3),
                          new("Deluxe",   140m, "FreeCancellation", ["WiFi","Breakfast"], 4),
                          new("Suite",    260m, "FreeCancellation", ["WiFi","Breakfast","Spa"], 5) ],
        ["bangalore"] = [ new("Standard",  85m, "FreeCancellation", ["WiFi"], 3),
                          new("Deluxe",   155m, "NonRefundable",    ["WiFi","Gym"], 4),
                          new("Suite",    290m, "FreeCancellation", ["WiFi","Gym","Pool"], 5) ],
        ["chennai"]   = [ new("Standard",  70m, "FreeCancellation", ["WiFi"], 3),
                          new("Deluxe",   130m, "FreeCancellation", ["WiFi","Breakfast"], 4),
                          new("Suite",    240m, "NonRefundable",    ["WiFi","Breakfast","Pool"], 4) ],
        ["hyderabad"] = [ new("Standard",  78m, "FreeCancellation", ["WiFi"], 3),
                          new("Deluxe",   145m, "FreeCancellation", ["WiFi","Breakfast"], 4),
                          new("Suite",    270m, "NonRefundable",    ["WiFi","Breakfast","Spa"], 5) ],
    };

    public Task<IEnumerable<AvailabilityResult>> SearchAsync(SearchParams p)
    {
        var key = p.Destination.ToLower();
        if (!Rooms.TryGetValue(key, out var rooms))
            return Task.FromResult(Enumerable.Empty<AvailabilityResult>());

        var nights = p.CheckOut.DayNumber - p.CheckIn.DayNumber;

        var results = rooms
            .Where(r => p.RoomType == null || r.Type == p.RoomType.ToString())
            .Select(r => new AvailabilityResult(
                Provider:            ProviderId,
                RoomType:            Enum.Parse<Models.RoomType>(r.Type),
                RatePerNight:        r.Rate,
                TotalPrice:          r.Rate * nights,
                Nights:              nights,
                CancellationPolicy:  Enum.Parse<CancellationPolicy>(r.Policy),
                Amenities:           r.Amenities,
                StarRating:          r.Stars
            ));

        return Task.FromResult(results);
    }

    private record StubRoom(string Type, decimal Rate, string Policy, string[] Amenities, int Stars);
}
