# Vertical Slice Ingredient List

This document serves as a **menu of options** for implementing a vertical slice. **NOT** every slice needs every item. Pick and choose what is required for the specific feature you are building.

## □ API Layer (The Entry Point)

_How the outside world interacts with this slice._

- [ ] **Endpoint(s)**: The controller action or Minimal API endpoint.
- [ ] **Request/Response DTOs**: Data contracts specific to this slice. Decouple domain from the wire.
- [ ] **Validation**: Input validation rules (e.g., FluentValidation) before processing.
- [ ] **Authorization**: `[Authorize]` attributes or policy checks at the endpoint level.
- [ ] **Idempotency**: Ensure safe retries for critical operations (e.g., payments, creates).

## □ Application Layer (The Orchestration)

_Coordinates the work, doesn't contain business rules._

- [ ] **Command/Query (CQRS)**: Split writes (Commands) from reads (Queries).
- [ ] **Handler**: The logic that executes the command/query.
- [ ] **Authorization**: Deeper resource-based auth (e.g., "Can THIS user edit THIS specific document?").
- [ ] **Mapping**: Slicing domain entities into DTOs (e.g., Mapster/AutoMapper or manual).
- [ ] **Logging**: Structured logs to trace the flow of this specific slice.
- [ ] **Transaction**: UnitOfWork scope to ensure atomicity.

## □ Domain Layer (The Heart)

_Pure business logic and rules. Ignorant of database/API._

- [ ] **Aggregate/Entity**: The core data and behavior model.
- [ ] **Domain Events**: Usage of `IDomainEvent` to decouple side effects (e.g., `CreateWorkspace`).
- [ ] **Business Rules**: Invariants that must always be true.
- [ ] **Domain Exceptions**: Specific error types for domain failures.
- [ ] **Audit fields**: Automatic tracking of `CreatedAt`, `UpdatedAt`, `CreatorId`, etc.

## □ Infrastructure Layer (The Plumbing)

_External concerns and actual data access._

- [ ] **Database access**: EF Core for complex writes/relationships, Dapper for high-performance reads.
- [ ] **External service clients**: Wrappers for 3rd party APIs if this slice talks to them.
- [ ] **Retry policies**: Resilience logic (e.g., Polly) for network calls.

## □ Optional (Add when slice needs it)

- [ ] **Cache**: Redis or Memory caching for expensive queries (Hybrid Cache).
- [ ] **Background Job**: Offloading slow work to Hangfire/Quartz.
- [ ] **SignalR**: Real-time updates to connected clients.
- [ ] **File storage**: Uploading/Retrieving blobs (S3, Azure Blob).
- [ ] **Email/SMS**: Notification dispatching.

## □ Cross-Cutting (Configure once, apply everywhere)

_Usually set up at project start, but check if slice needs specific config._

- [ ] **Rate limiting**: Prevent abuse.
- [ ] **CORS**: Browser security rules.
- [ ] **Health checks**: Monitoring probe points.
- [ ] **Global exception handling**: Standardize error responses.
- [ ] **Request/response compression**: Optimization.
- [ ] **API versioning**: Managing changes over time.
