using HotelStay.Api.Models;
using HotelStay.Api.Providers;
using HotelStay.Api.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// Serialize enums as strings
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

// Register providers — add new ones here without touching endpoints
builder.Services.AddSingleton<IHotelProvider, PremierStaysProvider>();
builder.Services.AddSingleton<IHotelProvider, BudgetNestsProvider>();
builder.Services.AddSingleton<HotelService>();

var app = builder.Build();
app.UseCors();

// ── GET /hotels/search ────────────────────────────────────────────────────────
app.MapGet("/hotels/search", async (
    [FromQuery] string? destination,
    [FromQuery] string? checkIn,
    [FromQuery] string? checkOut,
    [FromQuery] string? roomType,
    HotelService svc) =>
{
    // Required field validation
    if (string.IsNullOrWhiteSpace(destination) ||
        string.IsNullOrWhiteSpace(checkIn) ||
        string.IsNullOrWhiteSpace(checkOut))
        return Results.BadRequest(new { error = "destination, checkIn, and checkOut are required." });

    if (!DateOnly.TryParse(checkIn, out var ciDate))
        return Results.BadRequest(new { error = "Invalid checkIn date format. Use YYYY-MM-DD." });

    if (!DateOnly.TryParse(checkOut, out var coDate))
        return Results.BadRequest(new { error = "Invalid checkOut date format. Use YYYY-MM-DD." });

    if (coDate <= ciDate)
        return Results.BadRequest(new { error = "checkOut must be after checkIn." });

    RoomType? rt = null;
    if (!string.IsNullOrWhiteSpace(roomType))
    {
        if (!Enum.TryParse<RoomType>(roomType, true, out var parsed))
            return Results.BadRequest(new { error = "Invalid roomType. Valid values: Standard, Deluxe, Suite." });
        rt = parsed;
    }

    if (!svc.IsKnownDestination(destination))
        return Results.BadRequest(new { error = $"Unknown destination '{destination}'. Supported cities listed in /hotels/cities." });

    var p = new SearchParams(destination, ciDate, coDate, rt);
    var results = await svc.SearchAsync(p);

    return Results.Ok(results);
});

// ── POST /hotels/reserve ──────────────────────────────────────────────────────
app.MapPost("/hotels/reserve", async (
    [FromBody] ReservationRequestDto req,
    HotelService svc) =>
{
    if (string.IsNullOrWhiteSpace(req.GuestName))
        return Results.BadRequest(new { error = "guestName is required." });

    if (string.IsNullOrWhiteSpace(req.DocumentNumber))
        return Results.BadRequest(new { error = "documentNumber is required." });

    if (string.IsNullOrWhiteSpace(req.Destination))
        return Results.BadRequest(new { error = "destination is required." });

    if (!svc.IsKnownDestination(req.Destination))
        return Results.BadRequest(new { error = $"Unknown destination '{req.Destination}'." });

    if (!DateOnly.TryParse(req.CheckIn, out var ciDate))
        return Results.BadRequest(new { error = "Invalid checkIn date." });

    if (!DateOnly.TryParse(req.CheckOut, out var coDate))
        return Results.BadRequest(new { error = "Invalid checkOut date." });

    if (coDate <= ciDate)
        return Results.BadRequest(new { error = "checkOut must be after checkIn." });

    if (!Enum.TryParse<RoomType>(req.RoomType, true, out var roomType))
        return Results.BadRequest(new { error = "Invalid roomType." });

    if (!Enum.TryParse<DocumentType>(req.DocumentType, true, out var docType))
        return Results.BadRequest(new { error = "Invalid documentType." });

    // Server-side document validation
    var validation = svc.ValidateDocument(req.Destination, docType);
    if (!validation.IsValid)
        return Results.UnprocessableEntity(new { error = validation.Message });

    var provider = svc.GetProvider(req.ProviderId);
    if (provider == null)
        return Results.BadRequest(new { error = $"Unknown provider '{req.ProviderId}'." });

    var searchParams = new SearchParams(req.Destination, ciDate, coDate, roomType);
    var rooms = await provider.SearchAsync(searchParams);
    var room  = rooms.FirstOrDefault(r => r.RoomType == roomType);

    if (room == null)
        return Results.BadRequest(new { error = "Selected room is no longer available." });

    var domainReq = new ReservationRequest(
        req.Destination, ciDate, coDate, roomType,
        req.ProviderId, req.GuestName, docType, req.DocumentNumber);

    var reservation = svc.CreateReservation(domainReq, room);
    return Results.Ok(reservation);
});

// ── GET /hotels/reservation/{reference} ──────────────────────────────────────
app.MapGet("/hotels/reservation/{reference}", (string reference, HotelService svc) =>
{
    var reservation = svc.GetReservation(reference);
    return reservation is not null
        ? Results.Ok(reservation)
        : Results.NotFound(new { error = $"Reservation '{reference}' not found." });
});

// ── GET /hotels/cities — helper for frontend dropdowns ───────────────────────
app.MapGet("/hotels/cities", () => Results.Ok(new
{
    domestic      = new[] { "Mumbai", "Delhi", "Bangalore", "Chennai", "Hyderabad" },
    international = new[] { "Paris", "London", "New York", "Dubai", "Tokyo", "Sydney" }
}));

app.Run();

// DTO for reserve endpoint — accepts plain strings to avoid DateOnly/enum JSON parse issues
record ReservationRequestDto(
    string Destination,
    string CheckIn,
    string CheckOut,
    string RoomType,
    string ProviderId,
    string GuestName,
    string DocumentType,
    string DocumentNumber
);

// Expose for test project
public partial class Program { }
