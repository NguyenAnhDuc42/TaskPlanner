# TaskPlanner Technical Charter (Feature Implementation Guide)

This document defines the **Project Mindset**, the **Flow of Code**, and the **Architectural Tenets** that govern all development in TaskPlanner. It is the definitive source of truth for the project's production standards.

---

## 🏗️ 1. THE ARCHITECTURAL LAYERS (Pragmatic Dependency Inversion)

TaskPlanner follows a high-performance **Layered Vertical Slice Architecture**. We take a pragmatic approach to layering, favoring explicit "demands" via interfaces rather than dogmatic Clean Architecture constraints:

- **Domain Layer** (`server/Domain`): The heart of the system.
  - **Entities**: Pure business models with state and factory methods. We favor direct properties (primitives) over complex Value Objects where possible.
  - **Pragmatic Rule**: No Domain Events or Outbox patterns. Side effects are handled explicitly in the Application layer.

- **Application Layer** (`server/Application`): The "Toppest" Execution Layer.
  - **Responsibility**: Orchestrates features and defines what it needs from the lower layers.
  - **Pragmatic Rule**: Defines interfaces like `IDataBase` (which exposes a raw `IDbConnection` for Dapper). It does not handle implementation details.

- **Infrastructure Layer** (`server/Infrastructure`): The "Lowest" Implementation Layer.
  - **Responsibility**: Fulfills the demands of all upper layers.
  - **Pragmatic Rule**: Because it is the lowest, it can reference `Application` to implement its contracts. It contains the raw `Database` implementation, SignalR services, and EF Core configurations.

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
- **Pragmatic Rule**: Use `db.Connection` (Dapper) for all reads and bulk writes. Use `db.SaveChangesAsync()` for entity state changes.

### Step 3: Explicit Side Effects
Side effects (notifications, real-time updates) are called **explicitly** within the Handler after persistence.
- **Real-time**: Use `IRealtimeService` to notify users/workspaces immediately.
- **No Async/Outbox**: We favor immediate execution or direct service calls over event-driven background processing for core feature logic.

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
- **Primitive Favoritism**: Avoid custom Value Objects (e.g., `EntityName`, `HexColor`, `AuditInfo`). Use primitives (`string`, `Guid`, `DateTimeOffset`) directly on entities.
- **DateTimeOffset**: Always use `DateTimeOffset` for timestamps. DO NOT "clean up" `DateTimeOffset` into `DateTime`.
- **Query Simplicity**: Only use `.Include(...)` if the logic explicitly requires accessing related entity properties.

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
public class CreateSpaceHandler(IDataBase db, WorkspaceContext context, IRealtimeService realtime) 
    : ICommandHandler<CreateSpaceCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateSpaceCommand request, CancellationToken ct)
    {
        // 1. Permission Check via Context
        if (context.CurrentMember.Role > Role.Admin)
            return Result<Guid>.Failure(MemberError.DontHavePermission);

        // 2. Business Logic
        var slug = SlugHelper.GenerateSlug(request.name);
        var orderKey = await db.Spaces.ByWorkspace(context.workspaceId).GetNextOrderKey(ct);
        
        var space = ProjectSpace.Create(
            context.workspaceId,
            request.name,
            slug,
            context.CurrentMember.Id,
            orderKey
        );

        // 3. Persistence
        await db.Spaces.AddAsync(space, ct);
        await db.SaveChangesAsync(ct);
        
        // 4. Explicit Side Effects (Real-time)
        await realtime.NotifyWorkspaceAsync(context.workspaceId, "SpaceCreated", new { space.Id }, ct);

        return Result<Guid>.Success(space.Id);
    }
}
```
