# Reflection

## What I Would Improve With More Time

### Persistence
Replace the static in-memory dictionary with EF Core + SQLite so reservations survive restarts. The `HotelService` interface is already abstracted enough to swap this in.

### Real Async Provider Calls
The stubs return `Task.FromResult` synchronously. Real providers would use `HttpClient` with `IHttpClientFactory` and retry policies (Polly).

### Frontend Framework
The plain HTML/JS frontend works but a React or Angular app would provide better state management, especially for the sort/filter/pagination combinations.

### Pagination
The results list could grow large. Adding `page`/`pageSize` to the search endpoint and corresponding UI pagination would improve usability.

### Error Handling
More granular error types (provider timeout, partial results) rather than a single catch-all would give the frontend better feedback options.

### Integration Tests
Added unit tests for business logic. With more time I'd add `WebApplicationFactory`-based integration tests covering the full HTTP request/response cycle for all endpoints including 400/422 cases.

### HTTPS + CORS
Dev-only CORS `AllowAnyOrigin` is fine for this scope. Production would use a strict origin allowlist.

## What Went Well

- The `IHotelProvider` abstraction made adding the second stub trivial — extensibility requirement is fully met
- Deterministic stubs with known unavailable rooms made tests precise and meaningful
- Client-side + server-side dual validation follows defence-in-depth without duplicating logic
- `Task.WhenAll` for parallel provider queries is a small but real performance improvement
