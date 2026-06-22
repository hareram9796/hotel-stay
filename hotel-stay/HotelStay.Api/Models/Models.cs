namespace HotelStay.Api.Models;

public enum RoomType { Standard, Deluxe, Suite }

public enum CancellationPolicy { FreeCancellation, Flexible, NonRefundable }

public enum DocumentType { Passport, NationalId }

public record SearchParams(
    string Destination,
    DateOnly CheckIn,
    DateOnly CheckOut,
    RoomType? RoomType
);

public record AvailabilityResult(
    string Provider,
    RoomType RoomType,
    decimal RatePerNight,
    decimal TotalPrice,
    int Nights,
    CancellationPolicy CancellationPolicy,
    string[] Amenities,
    int? StarRating
);

public record ReservationRequest(
    string Destination,
    DateOnly CheckIn,
    DateOnly CheckOut,
    RoomType RoomType,
    string ProviderId,
    string GuestName,
    DocumentType DocumentType,
    string DocumentNumber
);

public record ReservationResponse(
    string ReferenceNumber,
    string Provider,
    string Destination,
    DateOnly CheckIn,
    DateOnly CheckOut,
    RoomType RoomType,
    decimal TotalPrice,
    CancellationPolicy CancellationPolicy,
    string GuestName
);
