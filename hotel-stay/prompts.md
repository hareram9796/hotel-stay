# AI Prompts & Key Decisions

## Prompts Used

### 1. Architecture Design
**Prompt:** "Design a hotel availability system with two stub providers (PremierStays full details, BudgetNests minimal), unified response model, document validation by city type, and a reservation flow. No auth, no real APIs, runs offline."

**Decision:** Used `IHotelProvider` interface with DI-injected stubs so adding a third provider is a one-line registration change. Chose Minimal API for low ceremony and fast iteration.

### 2. Provider Stub Design
**Prompt:** "Design deterministic stub data for PremierStays (PascalCase, always available, full details) and BudgetNests (snake_case source, may return available:false, rate/policy only). Cover all 11 cities with representative scenarios."

**Decision:** Kept stub data as in-memory dictionaries keyed by city name. Made specific BudgetNests rooms unavailable per city to ensure the filtering logic is actually exercised by tests.

### 3. Document Validation
**Prompt:** "Implement document validation: international cities require Passport, domestic cities accept NationalId or Passport. Return 422 on mismatch."

**Decision:** Validation lives in `HotelService` (not in the endpoint) so it's independently testable. Client-side warning shown immediately on doc type change; server-side validates again before saving.

### 4. Test Coverage
**Prompt:** "Write xUnit tests covering: document validation for all city types, BudgetNests filtering of unavailable rooms, total price calculation, reservation creation and retrieval, unknown destination handling."

**Decision:** Used `[Theory] + [InlineData]` for city classification tests to avoid repetition. Verified BudgetNests filtering using a specific city+roomType known to be unavailable in the stub.

### 5. Frontend UX
**Prompt:** "Build a clean search form with destination dropdown grouped by domestic/international, date pickers, optional room type filter. Results sortable by price. Reservation modal with client-side doc validation warning. Confirmation modal with reference number."

**Decision:** Used plain HTML/JS to keep the frontend zero-dependency and fully offline-capable. Client-side validation shows a warning (not a block) to guide the user; actual enforcement is server-side.

## Key Judgement Calls

- **Pagination:** Not implemented — scope says no persistence, stub data is small enough
- **Currency:** Used ₹ for display; rates are plain `decimal` in the model
- **Parallel provider queries:** Used `Task.WhenAll` so both providers are queried simultaneously
- **In-memory reservation store:** Static dictionary — sufficient for the scope, obvious upgrade path to a real DB
- **NationalId vs Passport for domestic:** Passport is also accepted (more permissive), which is realistic
