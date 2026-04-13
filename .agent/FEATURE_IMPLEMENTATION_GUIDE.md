# TaskPlanner Technical Charter (Feature Implementation Guide)

This document defines the **Project Mindset**, the **Flow of Code**, and the **Architectural Tenets** that govern all development in TaskPlanner. It is the definitive source of truth for the project's production standards.

---

## 🏗️ 1. THE ARCHITECTURAL LAYERS

TaskPlanner follows a high-performance **Layered Vertical Slice Architecture**. Each layer has a strict responsibility:

- **Domain Layer** (`server/Domain`): The heart of the system.
  - **Entities & Value Objects**: Pure business models with state and factory methods (e.g., `ProjectWorkspace.Create`).
  - **IQueryable Extensions**: Centralized query logic (e.g., `ByWorkspace`, `WhereNotDeleted`) to ensure consistency and facilitate future SQL optimization (Dapper/Raw SQL).
  - **Domain Events**: Internal signals indicating side effects need to happen.
- **Application Layer** (`server/Application`): The orchestration layer.
  - **Feature Slices**: Organized by folder, containing `Command/Query`, `Handler`, and `Validator`.
  - **Event Handlers**: Subscribers to Domain Events for async side effects (e.g., seeding data).
  - **Interfaces & Helpers**: Definition for data access (`IDataBase`) and identity (`WorkspaceContext`).
- **Infrastructure Layer** (`server/Infrastructure`): The implementation layer.
  - **Persistence**: EF Core implementation of `IDataBase`, Configuration, and Migrations.
  - **Services**: Implementations for `IRealtimeService` (SignalR) and `HybridCache`.
- **Background Layer** (`server/Background`): The out-of-process layer.
  - **Jobs & Workers**: Hangfire jobs (e.g., `ProcessOutboxJob`) and cleanup workers.

---

## 📂 2. THE ANATOMY OF A SLICE

We organize code by **feature capability**, using a **Feature Folder** pattern instead of type-based folders.

- **The Folder-per-Feature Rule**: Each feature lives in its own directory:
  `Application/Features/[Module]/[SubModule]/[Feature]/`
- **Standard Components**:
  - `[Feature]Command.cs` / `[Feature]Query.cs`: The request record implementation.
  - `[Feature]Handler.cs`: The core execution logic.
  - `[Feature]Validator.cs`: FluentValidation rules for the request.
- **One Class Per File**: DO NOT merge Commands, Responses, or Handlers into a single file. Each component must reside in its own dedicated `.cs` file.
- **DTOs**: Shared DTOs should be placed in a `Common` or `Response` folder within the parent module if reused across slices.

---

## 🛡️ 3. THE EXECUTION FLOW (Life of a Request)

### Step 1: Resolution & Context Entry
The request enters the API. `WorkspaceContext` resolves the "Truth" (Who is the user? Which workspace? What is their role?).
- **Rule**: Access `context.CurrentMember` for permission checks.

### Step 2: Validation & Execution
The request is validated via FluentValidation. The Handler executes business logic using:
- **Domain Extensions**: Use `_db.Entities.ByWorkspace(id).WhereNotDeleted()` for safe access.
- **Entity Factories**: Create entities via static `Entity.Create(...)` methods.

### Step 3: Performance & Side-Effects
- **Caching**: Use `HybridCache` to invalidate tags or update shared state.
- **Real-time**: Notify clients immediately using `IRealtimeService`.

### Step 4: Persistence & Outbox
The handler calls `_db.SaveChangesAsync()`.
- **Domain Events** are collected and saved as `OutboxMessages` in the same DB transaction.
- CORE data is committed.

### Step 5: Background Processing (The Jobs Layer)
The **Background Layer** picks up the Outbox messages.
- `ProcessOutboxJob` enqueues handlers of `IDomainEventHandler<TEvent>`.
- Side effects (e.g., seeding, cascading updates) happen asynchronously.

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
