# TaskPlanner Technical Charter (Feature Implementation Guide)

This document defines the **Project Mindset**, the **Flow of Code**, and the **Architectural Tenets** that govern all development in TaskPlanner. It is the definitive source of truth for the project's production standards.

---

## 🏗️ 1. THE ARCHITECTURAL LAYERS (Pragmatic Dependency Inversion)

TaskPlanner follows a high-performance **Layered Vertical Slice Architecture**. We take a pragmatic approach to layering, favoring explicit "demands" via interfaces rather than dogmatic Clean Architecture constraints:

- **Domain Layer** (`server/Domain`): The heart of the system.
  - **Entities & Value Objects**: Pure business models with state and factory methods.
  - **Domain Events**: Internal signals indicating side effects need to happen.

- **Application Layer** (`server/Application`): The "Toppest" Execution Layer.
  - **Responsibility**: Orchestrates features and defines what it needs from the lower layers.
  - **Pragmatic Rule**: Defines interfaces like `IDataBase` (which exposes a raw `IDbConnection` for Dapper) and `IBackgroundJobService`. It does not handle implementation details.

- **Background Layer** (`server/Background`): The Out-of-Process Layer.
  - **Responsibility**: Executes Hangfire jobs, heavy cleanup operations, and external API calls.
  - **Pragmatic Rule**: It defines its own interfaces (`Background.Interfaces`) to demand what it needs. **Crucially, it has ZERO reference to the Infrastructure project.** It cannot call interfaces from Application.

- **Infrastructure Layer** (`server/Infrastructure`): The "Lowest" Implementation Layer.
  - **Responsibility**: Fulfills the demands of all upper layers.
  - **Pragmatic Rule**: Because it is the lowest, it can reference `Application` and `Background` to implement their contracts. It contains the raw `Database` implementation, SignalR services, and the local worker that triggers the outbox.

---

## 📂 2. THE ANATOMY OF A SLICE

We organize code by **feature capability**, using a **Feature Folder** pattern instead of type-based folders.

- **The Folder-per-Feature Rule**: Each feature lives in its own directory:
  `Application/Features/[Module]/[SubModule]/[Feature]/`
- **Standard Components**:
  - `[Feature]Command.cs` / `[Feature]Query.cs`: The request record implementation.
  - `[Feature]Handler.cs`: The core execution logic.
  - `[Feature]Validator.cs`: FluentValidation rules for the request.
  - `[Feature]SQL.cs`: (Required for Queries and Bulk Actions) Contains raw SQL strings or Dapper logic.
- **One Class Per File**: Each component (Command/Query, Handler, Validator, SQL) must reside in its own dedicated `.cs` file. DTOs should be defined within the Command/Query file if they are specific to that feature.
- **DTOs**: Shared DTOs should be placed in a `Common` or `Response` folder within the parent module if reused across slices.

---

## 🛡️ 3. THE EXECUTION FLOW (Life of a Request)

### Step 1: Resolution & Context Entry
The request enters the API. `WorkspaceContext` resolves the "Truth" (Who is the user? Which workspace? What is their role?).
- **Rule**: Access `context.CurrentMember` for permission checks.

### Step 2: Validation & Logic
The request is validated via FluentValidation. The Handler executes logic.
- **Pragmatic Rule**: Use `db.Connection` (Dapper) for all reads and bulk writes. Use `db.SaveChangesAsync()` only for complex entity state changes.

### Step 3: Persistence & Instant Outbox
- **EF ChangeTracker**: Automatically collects domain events during `SaveChangesAsync`.
- **Instant Signal**: `Database.cs` sends an in-memory signal (via a .NET Channel) to the `LocalOutboxWorker` the moment the transaction commits. **NO POLLING DELAY.**

### Step 4: Background Processing
The **Local Worker** (in Infrastructure) wakes up instantly and calls the `ProcessOutboxJob` (in Background).
- Side effects (emails, notifications, cascading deletes) run out-of-process.
- **Hangfire**: Acts as a safety net (set to 30m+ intervals) to catch any missed messages if the server restarts.

---

## 🔑 4. IDENTITY & TENANCY RULES (CRITICAL)

Strict adherence to workspace tenancy is non-negotiable.

- **MemberId vs UserId**: 
  - All workspace-bound entities (Tasks, Folders, ChatRooms, Attachments, etc.) MUST be owned by a **WorkspaceMember**. 
  - Use `context.CurrentMember.Id` (The Member ID) for `creatorId` and authorization.
  - Global `UserId` (`context.CurrentMember.UserId`) should ONLY be used for identity-level operations (Sessions, Login) or cross-workspace lookup.
- **Tenancy Enforcement**: 
  - Any operation involving a list of users (e.g., inviting to a ChatRoom, assigning a Task) MUST validate that those users are already members of the relevant workspace. 
  - Validate against `db.Members.ByWorkspace(workspaceId)` before creating relations.

---

## ⚙️ 5. TYPE & LOGIC PRESERVATION

- **NO Logic-Altering Refactors**: Standardization passes MUST NOT change underlying business logic or data types.
- **DateTimeOffset**: Always use `DateTimeOffset` for timestamps. DO NOT "clean up" `DateTimeOffset` into `DateTime`.
- **Query Simplicity**: Only use `.Include(...)` if the logic explicitly requires accessing related entity properties. Do not add them for "future use" or "safety" if not needed.

---

## 📝 6. REFERENCE ANATOMY (The "Perfect" Feature Slice)

### 1. Folder Structure
```text
Application/Features/SpaceFeatures/SelfManagement/CreateSpace/
├── CreateSpaceCommand.cs
├── CreateSpaceHandler.cs
└── CreateSpaceValidator.cs
```

### 2. The Handler (CreateSpaceHandler.cs)
```csharp
public class CreateSpaceHandler(IDataBase db, WorkspaceContext context, HybridCache cache) 
    : ICommandHandler<CreateSpaceCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateSpaceCommand request, CancellationToken ct)
    {
        // 1. Permission Check via Context
        if (context.CurrentMember.Role > Role.Admin)
            return Result<Guid>.Failure(MemberError.DontHavePermission);

        // 2. Business Logic with Domain Extensions
        var orderKey = await db.Spaces.ByWorkspace(context.workspaceId).GetNextOrderKey(ct);
        
        var space = ProjectSpace.Create(
            context.workspaceId,
            request.name,
            context.CurrentMember.Id, // MemberId, NOT UserId
            orderKey
        );

        // 3. Persistence & Outbox (Events are internal to entity)
        await db.Spaces.AddAsync(space, ct);
        await db.SaveChangesAsync(ct);
        
        // 4. Performance: Cache Invalidation
        await cache.RemoveByTagAsync(SpaceCacheKeys.WorkspaceSpaceTag(context.workspaceId), ct);

        return Result<Guid>.Success(space.Id);
    }
}
```
