# Hotel Stay Availability — Spec

## Data Models

### Unified Room Type Enum
```
Standard | Deluxe | Suite
```

### Unified Availability Response
```json
{
  "provider": "PremierStays | BudgetNests",
  "roomType": "Standard | Deluxe | Suite",
  "ratePerNight": 120.00,
  "totalPrice": 360.00,
  "nights": 3,
  "cancellationPolicy": "FreeCancellation | Flexible | NonRefundable",
  "amenities": ["WiFi", "Breakfast"],
  "starRating": 4
}
```

### Reservation Request
```json
{
  "destination": "Paris",
  "checkIn": "2025-08-01",
  "checkOut": "2025-08-04",
  "roomType": "Deluxe",
  "providerId": "PremierStays",
  "guestName": "John Doe",
  "documentType": "Passport | NationalId",
  "documentNumber": "AB123456"
}
```

### Reservation Response
```json
{
  "referenceNumber": "SKY-XXXXXXXX",
  "provider": "PremierStays",
  "destination": "Paris",
  "checkIn": "2025-08-01",
  "checkOut": "2025-08-04",
  "roomType": "Deluxe",
  "totalPrice": 360.00,
  "cancellationPolicy": "FreeCancellation",
  "guestName": "John Doe"
}
```

## IHotelProvider Interface Contract
```csharp
interface IHotelProvider {
    string ProviderId { get; }
    Task<IEnumerable<AvailabilityResult>> SearchAsync(SearchParams p);
}
```

## City Classification
- Domestic: Mumbai, Delhi, Bangalore, Chennai, Hyderabad
- International: Paris, London, New York, Dubai, Tokyo, Sydney

## Document Rules
- International destination → Passport required
- Domestic destination → NationalId accepted (Passport also accepted)

## API Endpoints
- GET  /hotels/search?destination=&checkIn=&checkOut=&roomType=
- POST /hotels/reserve
- GET  /hotels/reservation/{reference}

## Validation Rules
- destination, checkIn, checkOut required → 400
- checkOut must be after checkIn → 400
- document mismatch for destination → 422
- roomType optional, defaults to all types

## Provider Stub Behaviour

### PremierStays (PascalCase, always available)
- Returns full details: rate, cancellation, amenities, star rating
- CancellationPolicy: "FreeCancellation" or "NonRefundable"

### BudgetNests (snake_case, may return available: false)
- Returns rate and policy only
- CancellationPolicy: "Flexible" or "NonRefundable"
- Filter out rooms where available == false

## Extensibility
- New providers implement IHotelProvider and register in DI — no core changes needed
