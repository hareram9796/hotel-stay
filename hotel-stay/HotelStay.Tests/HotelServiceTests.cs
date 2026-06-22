using HotelStay.Api.Models;
using HotelStay.Api.Providers;
using HotelStay.Api.Services;
using Xunit;

namespace HotelStay.Tests;

public class HotelServiceTests
{
    private static HotelService BuildService() =>
        new HotelService([new PremierStaysProvider(), new BudgetNestsProvider()]);

    // ── Document Validation ───────────────────────────────────────────────────

    [Fact]
    public void International_RequiresPassport_NationalId_Fails()
    {
        var svc = BuildService();
        var result = svc.ValidateDocument("Paris", DocumentType.NationalId);
        Assert.False(result.IsValid);
        Assert.Contains("Passport", result.Message);
    }

    [Fact]
    public void International_WithPassport_Passes()
    {
        var svc = BuildService();
        var result = svc.ValidateDocument("Paris", DocumentType.Passport);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Domestic_NationalId_Passes()
    {
        var svc = BuildService();
        var result = svc.ValidateDocument("Mumbai", DocumentType.NationalId);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Domestic_Passport_AlsoAccepted()
    {
        var svc = BuildService();
        var result = svc.ValidateDocument("Delhi", DocumentType.Passport);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("London")]
    [InlineData("New York")]
    [InlineData("Dubai")]
    [InlineData("Tokyo")]
    [InlineData("Sydney")]
    public void AllInternationalCities_RequirePassport(string city)
    {
        var svc = BuildService();
        var result = svc.ValidateDocument(city, DocumentType.NationalId);
        Assert.False(result.IsValid);
    }

    // ── City Classification ───────────────────────────────────────────────────

    [Theory]
    [InlineData("Mumbai")]
    [InlineData("Delhi")]
    [InlineData("Bangalore")]
    [InlineData("Chennai")]
    [InlineData("Hyderabad")]
    public void DomesticCities_AreRecognised(string city)
    {
        var svc = BuildService();
        Assert.True(svc.IsDomestic(city));
        Assert.False(svc.IsInternational(city));
    }

    [Theory]
    [InlineData("Paris")]
    [InlineData("London")]
    [InlineData("New York")]
    [InlineData("Dubai")]
    [InlineData("Tokyo")]
    [InlineData("Sydney")]
    public void InternationalCities_AreRecognised(string city)
    {
        var svc = BuildService();
        Assert.True(svc.IsInternational(city));
        Assert.False(svc.IsDomestic(city));
    }

    [Fact]
    public void UnknownCity_IsNotRecognised()
    {
        var svc = BuildService();
        Assert.False(svc.IsKnownDestination("Atlantis"));
    }

    // ── Search & Provider Filtering ───────────────────────────────────────────

    [Fact]
    public async Task Search_ReturnsBothProviders()
    {
        var svc = BuildService();
        var p = new SearchParams("Paris", new DateOnly(2025, 8, 1), new DateOnly(2025, 8, 4), null);
        var results = (await svc.SearchAsync(p)).ToList();
        Assert.Contains(results, r => r.Provider == "PremierStays");
        Assert.Contains(results, r => r.Provider == "BudgetNests");
    }

    [Fact]
    public async Task BudgetNests_FiltersUnavailableRooms()
    {
        // Paris Suite from BudgetNests is marked unavailable in stub
        var provider = new BudgetNestsProvider();
        var p = new SearchParams("Paris", new DateOnly(2025, 8, 1), new DateOnly(2025, 8, 4), RoomType.Suite);
        var results = (await provider.SearchAsync(p)).ToList();
        Assert.Empty(results);
    }

    [Fact]
    public async Task PremierStays_AlwaysReturnsRooms()
    {
        var provider = new PremierStaysProvider();
        var p = new SearchParams("Paris", new DateOnly(2025, 8, 1), new DateOnly(2025, 8, 4), null);
        var results = (await provider.SearchAsync(p)).ToList();
        Assert.Equal(3, results.Count); // Standard, Deluxe, Suite
    }

    [Fact]
    public async Task Search_RoomTypeFilter_Works()
    {
        var svc = BuildService();
        var p = new SearchParams("London", new DateOnly(2025, 9, 1), new DateOnly(2025, 9, 3), RoomType.Standard);
        var results = (await svc.SearchAsync(p)).ToList();
        Assert.All(results, r => Assert.Equal(RoomType.Standard, r.RoomType));
    }

    [Fact]
    public async Task Search_UnknownDestination_ReturnsEmpty()
    {
        var svc = BuildService();
        var p = new SearchParams("Atlantis", new DateOnly(2025, 8, 1), new DateOnly(2025, 8, 3), null);
        var results = await svc.SearchAsync(p);
        Assert.Empty(results);
    }

    // ── Price Calculation ─────────────────────────────────────────────────────

    [Fact]
    public async Task TotalPrice_IsRatePerNight_Times_Nights()
    {
        var provider = new PremierStaysProvider();
        var p = new SearchParams("Mumbai", new DateOnly(2025, 8, 1), new DateOnly(2025, 8, 4), RoomType.Standard);
        var results = (await provider.SearchAsync(p)).ToList();
        var room = results.First();
        Assert.Equal(3, room.Nights);
        Assert.Equal(room.RatePerNight * 3, room.TotalPrice);
    }

    // ── Reservation ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateReservation_ReturnsReferenceNumber()
    {
        var svc = BuildService();
        var room = new AvailabilityResult("PremierStays", RoomType.Deluxe, 200m, 600m, 3,
            CancellationPolicy.FreeCancellation, ["WiFi"], 5);
        var req = new ReservationRequest("Paris", new DateOnly(2025, 8, 1), new DateOnly(2025, 8, 4),
            RoomType.Deluxe, "PremierStays", "John Doe", DocumentType.Passport, "AB123456");

        var reservation = svc.CreateReservation(req, room);

        Assert.StartsWith("SKY-", reservation.ReferenceNumber);
        Assert.Equal("John Doe", reservation.GuestName);
        Assert.Equal(600m, reservation.TotalPrice);
    }

    [Fact]
    public async Task GetReservation_ReturnsStoredReservation()
    {
        var svc = BuildService();
        var room = new AvailabilityResult("BudgetNests", RoomType.Standard, 60m, 180m, 3,
            CancellationPolicy.Flexible, [], null);
        var req = new ReservationRequest("Mumbai", new DateOnly(2025, 8, 1), new DateOnly(2025, 8, 4),
            RoomType.Standard, "BudgetNests", "Priya Sharma", DocumentType.NationalId, "MH123456");

        var created = svc.CreateReservation(req, room);
        var fetched = svc.GetReservation(created.ReferenceNumber);

        Assert.NotNull(fetched);
        Assert.Equal(created.ReferenceNumber, fetched.ReferenceNumber);
    }

    [Fact]
    public void GetReservation_NonExistentRef_ReturnsNull()
    {
        var svc = BuildService();
        Assert.Null(svc.GetReservation("SKY-NOTREAL"));
    }
}
