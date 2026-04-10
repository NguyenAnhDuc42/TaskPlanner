# TaskPlanner Technical Charter (Feature Implementation Guide)

This document defines the **Project Mindset**, the **Flow of Code**, and the **Architectural Tenets** that govern all development in TaskPlanner. It is the definitive source of truth for the project's production standards.

---

## 💎 1. THE ARCHITECTURAL TENETS (The Mindset)

### 🔄 A. Aggressive Centralization
If any logic—be it a recursive SQL check, a validation rule, or a security boundary—is required by more than one feature, it MUST be centralized. 
- **Rule**: Never repeat "patch-up" code. Move it to the most central layer suitable (e.g., `WorkspaceContext`, `IDomainService`, or a `Decorator`).
- **Goal**: One change in a centralized rule must propagate throughout the entire system.

### 🚫 B. Expected Failures as Data (Explicit Errors)
We treat foreseeable failures (NotFound, Unauthorized, Validation) as **Data**, not as **System Exceptions**.
- **Rule**: Mandatory usage of the `Result` and `Result<T>` system. 
- **Exceptions**: Reserved strictly for catastrophic system failures (e.g., Database Connection lost).
- **Enforcement**: If you see a `throw new Exception` for business logic, refactor it to a `Result.Failure`.

### 🎭 C. Pure Handler Responsibility
A Command/Query Handler should focus 100% on the **"Perfect Path"** logic of the business feature.
- **Rule**: Cross-cutting concerns (Validation, Security Context, Logging, Transaction Management) MUST be handled by **Decorators** or **Pipeline Behaviors**.
- **Result**: Handlers remain lean, readable, and easy to test.

---

## 🏗️ 2. THE ANATOMY OF A SLICE (Vertical Slice Architecture)

We organize code by **what it does**, not **what type of file it is**.

- **The Single File Rule**: A feature slice (e.g., `GetHierarchy.cs`) SHOULD contain the Command/Query record, the DTOs, SQL Query, and the Handler logic in one cohesive unit.
- **Organization**: Group slices by feature module: `Application/Features/[Module]/[SubModule]/[Feature]/`.

---

## 🛡️ 3. THE EXECUTION FLOW (The Life of a Request)

### Step 1: Resolution (The Context Entry)
The request enters the API. The **Security Boundary** (e.g., `WorkspaceContext`) resolves the "Truth" of the request:
- *Who* is the user? 
- *Which* Workspace are they in? 
- *What* is their Member identity? 
- **Rule**: All security resolution happens here. Handlers receive resolved IDs, not raw HttpContext.

### Step 2: Validation & Guarding (The Decorator Layer)
The request is validated via FluentValidation and guarded by context-checks (e.g., `ValidationDecorator`). If failed, a `Result.Failure` is returned immediately.

### Step 3: Execution (The Pure Handler)
The Handler executes the business logic using the settled IDs from the Context.
- Uses `IDataBase` named properties.
- Uses **Domain Extensions** (`WhereNotDeleted()`, `ByWorkspace()`) for data operations.

### Step 4: Persistence & Side-Effects (Option B)
The Handler calls `_db.SaveChangesAsync()`. 
- CORE data is committed.
- Domain Events are captured in the **Outbox**.
- **Background Jobs** (Seeding, Cleanup, Real-time) are enqueued immediately.

---

## 📝 4. REFERENCE ANATOMY (The "Perfect" Query Slice)

```csharp
namespace Application.Features.WorkspaceFeatures.HierarchyManagement;

// 1. DTOs and Logic live together
public record WorkspaceHierarchyDto(Guid Id, string Name, List<SpaceDto> Spaces);
public record SpaceDto(Guid Id, string Name, string Color);

// 2. The Query Request
public record GetHierarchyQuery(Guid WorkspaceId) : IQueryRequest<WorkspaceHierarchyDto>;

// 3. The Handler (Focused strictly on data retrieval)
public class GetHierarchyHandler : IQueryHandler<GetHierarchyQuery, WorkspaceHierarchyDto>
{
    private readonly IDataBase _db;

    public GetHierarchyHandler(IDataBase db) => _db = db;

    public async Task<Result<WorkspaceHierarchyDto>> Handle(GetHierarchyQuery request, CancellationToken ct)
    {
        // Use Domain Extensions for safe, centralized logic
        var workspace = await _db.Workspaces
            .AsNoTracking()
            .WhereNotDeleted()
            .ProjectToDto() // Centralized projection
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId, ct);

        return workspace ?? Error.NotFound("Workspace.NotFound", "Workspace not found.");
    }
}
```
