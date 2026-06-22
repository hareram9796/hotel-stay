# Hotel Stay Availability — SkyRoute

Full-stack hotel availability and reservation system built on .NET 8 Minimal API + plain HTML/JS frontend.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- A modern browser (Chrome, Edge, Firefox)

## Run the Application

### 1. Start the API

```bash
cd HotelStay.Api
dotnet run
```

API starts on **http://localhost:5000**  
Swagger/OpenAPI docs: **http://localhost:5000/swagger** (development mode)

### 2. Open the Frontend

Open `hotel-ui/index.html` directly in your browser:

```
file:///path/to/hotel-stay/hotel-ui/index.html
```

Or serve it with any static server:

```bash
cd hotel-ui
npx serve .
# then open http://localhost:3000
```

### 3. Run Tests

```bash
cd HotelStay.Tests
dotnet test
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/hotels/search?destination=Paris&checkIn=2025-08-01&checkOut=2025-08-04` | Search available rooms |
| GET | `/hotels/search?...&roomType=Deluxe` | Filter by room type (optional) |
| POST | `/hotels/reserve` | Reserve a room |
| GET | `/hotels/reservation/{reference}` | Get reservation by reference |
| GET | `/hotels/cities` | List supported cities |

## Destinations

| Type | Cities |
|------|--------|
| 🇮🇳 Domestic | Mumbai, Delhi, Bangalore, Chennai, Hyderabad |
| 🌍 International | Paris, London, New York, Dubai, Tokyo, Sydney |

## Document Rules

- International → **Passport** required
- Domestic → **National ID** accepted (Passport also valid)
- Mismatch returns `422 Unprocessable Entity`

## Architecture

```
hotel-stay/
├── HotelStay.Api/
│   ├── Models/Models.cs          — shared data models & enums
│   ├── Providers/
│   │   ├── IHotelProvider.cs     — provider interface (extensibility point)
│   │   ├── PremierStaysProvider  — stub: full details, always available
│   │   └── BudgetNestsProvider   — stub: minimal, filters available:false
│   ├── Services/HotelService.cs  — business logic, validation, reservation store
│   └── Program.cs                — Minimal API endpoints + DI registration
├── HotelStay.Tests/
│   └── HotelServiceTests.cs      — xUnit tests for core logic
├── hotel-ui/
│   ├── index.html                — search, results, modals
│   ├── style.css
│   └── app.js                    — API calls, client-side validation
├── spec.md                       — committed before implementation
├── prompts.md
└── reflection.md
```

## Adding a Third Provider

1. Create `Providers/NewProvider.cs` implementing `IHotelProvider`
2. Register in `Program.cs`: `builder.Services.AddSingleton<IHotelProvider, NewProvider>()`
3. No other changes needed — the service aggregates all registered providers automatically
