# Comprehensive Feature Audit Ledger

This ledger acts as a master checklist for every handler in the system against the `ARCHITECTURE.md` standard. Do not delete features when fixing them; instead, change the `❌` to `✅`.

## Criteria Key
- **[REALTIME]**: Uses `EntityBatchUpdate/Delete` (Commands only)
- **[PERM]**: Uses `PermissionService` (No manual role/DB checks)
- **[CQRS]**: Returns normalized `Result` (No `Result<Guid>` for mutations)
- **[OPTIMIZE]**: Uses `FirstOrDefaultAsync` (No `FindAsync`)
- **[TENANT]**: Explicit `@WorkspaceId` isolation (Dapper Queries)
- **[CACHE]**: Uses backend caching or write-through invalidation

---

## Workspace Features

### UpdateWorkspaceHandler.cs (Command)
- **[REALTIME]** ❌ Does not broadcast `EntityBatchUpdate`.
- **[PERM]** ❌ Manual `CurrentMember.Role` check.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### TransferOwnershipHandler.cs (Command)
- **[REALTIME]** ❌ Does not broadcast `EntityBatchUpdate`.
- **[PERM]** ❌ Manual `CurrentMember.Role` check.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### AddMembersHandler.cs (Command)
- **[REALTIME]** ❌ Does not broadcast `EntityBatchUpdate`.
- **[PERM]** ❌ Manual `CurrentMember.Role` check.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### RemoveMembersHandler.cs (Command)
- **[REALTIME]** ❌ Does not broadcast `EntityBatchDelete`.
- **[PERM]** ❌ Manual `CurrentMember.Role` check.
- **[CQRS]** ❌ Returns `Result<Guid>`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### UpdateMembersHandler.cs (Command)
- **[REALTIME]** ❌ Does not broadcast `EntityBatchUpdate`.
- **[PERM]** ❌ Manual `CurrentMember.Role` check.
- **[CQRS]** ❌ Returns `Result<Guid>`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### MoveItemHandler.cs (Command)
- **[REALTIME]** ❌ Uses `NotifyWorkspaceAsync`.
- **[PERM]** ❌ Manual `CurrentMember.Role` check.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### BatchMoveItemHandler.cs (Command)
- **[REALTIME]** ✅ Uses `EntityBatch`.
- **[PERM]** ❌ Manual `CurrentMember.Role` check.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### JoinWorkspaceByCodeHandler.cs (Command)
- **[REALTIME]** ❌ Does not broadcast `EntityBatchUpdate`.
- **[PERM]** ❌ Missing `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### LeaveWorkspaceHandler.cs (Command)
- **[REALTIME]** ❌ Does not broadcast `EntityBatchUpdate`.
- **[PERM]** ❌ Missing `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### SetWorkspacePinHandler.cs (Command)
- **[REALTIME]** ❌ Does not broadcast `EntityBatchUpdate`.
- **[PERM]** ❌ Missing `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### DeleteWorkspaceHandler.cs (Command)
- **[REALTIME]** ❌ Does not broadcast `EntityBatchDelete`.
- **[PERM]** ❌ Missing `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### CreateWorkspaceHandler.cs (Command)
- **[REALTIME]** ❌ Does not broadcast `EntityBatchUpdate`.
- **[PERM]** ❌ Missing `PermissionService`.
- **[CQRS]** ❌ Returns `Result<Guid>`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

---

## Space Features

### CreateSpaceHandler.cs (Command)
- **[REALTIME]** ❌ Uses `NotifyWorkspaceAsync`.
- **[PERM]** ❌ Manual `db.EntityAccesses` check.
- **[CQRS]** ❌ Returns `Result<Guid>`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### UpdateSpaceHandler.cs (Command)
- **[REALTIME]** ❌ Uses `NotifyWorkspaceAsync`.
- **[PERM]** ❌ Missing `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### DeleteSpaceHandler.cs (Command)
- **[REALTIME]** ❌ Uses `NotifyWorkspaceAsync`.
- **[PERM]** ❌ Missing `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### BatchUpdateItemsHandler.cs (Command)
- **[REALTIME]** ❌ Does not broadcast `EntityBatchUpdate`.
- **[PERM]** ❌ Missing `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### CreateSpaceDocumentHandler.cs (Command)
- **[REALTIME]** ❌ Does not broadcast `EntityBatchUpdate`.
- **[PERM]** ❌ Missing `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

---

## Folder Features

### CreateFolderHandler.cs (Command)
- **[REALTIME]** ❌ Uses `NotifyWorkspaceAsync`.
- **[PERM]** ❌ Missing `PermissionService`.
- **[CQRS]** ❌ Returns `Result<Guid>`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### UpdateFolderHandler.cs (Command)
- **[REALTIME]** ❌ Uses `NotifyWorkspaceAsync`.
- **[PERM]** ❌ Missing `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### DeleteFolderHandler.cs (Command)
- **[REALTIME]** ❌ Uses `NotifyWorkspaceAsync`.
- **[PERM]** ❌ Missing `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### BatchUpdateFolderTasksHandler.cs (Command)
- **[REALTIME]** ❌ Uses `NotifyWorkspaceAsync`.
- **[PERM]** ❌ Missing `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

---

## Task Features

### CreateTaskHandler.cs (Command)
- **[REALTIME]** ❌ Uses `NotifyWorkspaceAsync`.
- **[PERM]** ❌ Missing `PermissionService`.
- **[CQRS]** ❌ Returns `Result<Guid>`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### UpdateTaskHandler.cs (Command)
- **[REALTIME]** ❌ Uses `NotifyWorkspaceAsync`.
- **[PERM]** ❌ Missing `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### DeleteTaskHandler.cs (Command)
- **[REALTIME]** ❌ Uses `NotifyWorkspaceAsync`.
- **[PERM]** ❌ Missing `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### UpdateTaskAssigneesHandler.cs (Command)
- **[REALTIME]** ✅ Uses `EntityBatch`.
- **[PERM]** ❌ Missing `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### AddCommentHandler.cs (Command)
- **[REALTIME]** ❌ Does not broadcast `EntityBatchUpdate`.
- **[PERM]** ❌ Missing `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### GetTaskAssigneesHandler.cs (Query)
- **[PERF]** ❌ Uses `FindAsync`. Should be `FirstOrDefaultAsync`.

### CreateSubTaskHandler.cs (Command)
- **[REALTIME]** ✅ Uses `EntityBatchUpdate`.
- **[PERM]** ✅ Uses `PermissionService`.
- **[CQRS]** ✅ Returns `Result<TaskRecord>`.
- **[OPTIMIZE]** ✅ Uses `FirstOrDefaultAsync`.
- **[CACHE]** ⚠️ Needs verification.

### UpdateSubTaskHandler.cs (Command)
- **[REALTIME]** ✅ Uses `EntityBatchUpdate`.
- **[PERM]** ✅ Uses `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Uses `FirstOrDefaultAsync`.
- **[CACHE]** ⚠️ Needs verification.

### DeleteSubTaskHandler.cs (Command)
- **[REALTIME]** ✅ Uses `EntityBatchDelete`.
- **[PERM]** ✅ Uses `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Uses `FirstOrDefaultAsync`.
- **[CACHE]** ⚠️ Needs verification.

---

## Workflow Features

### UpdateWorkflowStatusesHandler.cs (Command)
- **[REALTIME]** ❌ Uses `NotifyWorkspaceAsync`.
- **[PERM]** ❌ Manual `CurrentMember.Role` check.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### ReorderStatusesHandler.cs (Command)
- **[REALTIME]** ❌ Uses `NotifyWorkspaceAsync`.
- **[PERM]** ❌ Manual `CurrentMember.Role` check.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### SetLayerWorkflowHandler.cs (Command)
- **[REALTIME]** ❌ Does not broadcast `EntityBatchUpdate`.
- **[PERM]** ❌ Manual `CurrentMember.Role` check.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

---

## Entity Access Features

### EntitAccessBatchHandler.cs (Command)
- **[REALTIME]** ❌ Does not broadcast `EntityBatchUpdate`.
- **[PERM]** ❌ Manual `db.EntityAccesses` check.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

---

## Document Features

### UpdateDocumentBlocksHandler.cs (Command)
- **[REALTIME]** ❌ Does not broadcast `EntityBatchUpdate`.
- **[PERM]** ❌ Missing `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

---

## Attachment Features

### UploadAttachmentHandler.cs (Command)
- **[REALTIME]** ❌ Does not broadcast `EntityBatchUpdate`.
- **[PERM]** ❌ Manual `CurrentMember.Role` check.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### LinkAttachmentHandler.cs (Command)
- **[REALTIME]** ❌ Does not broadcast `EntityBatchUpdate`.
- **[PERM]** ❌ Manual `CurrentMember.Role` check.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### DeleteAttachmentHandler.cs (Command)
- **[REALTIME]** ❌ Does not broadcast `EntityBatchDelete`.
- **[PERM]** ❌ Manual `CurrentMember.Role` check.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

---

## Auth Features
*(Auth features manage external boundaries. Realtime sync is mostly N/A)*

### ChangePasswordHandler.cs (Command)
- **[PERM]** N/A
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ❌ Uses `FindAsync`. Should be `FirstOrDefaultAsync`.

### Login/Register/Logout/ForgotPassword/ResetPassword/ExternalLogin/RefreshToken/UpdateProfile
- **[OPTIMIZE]** ✅ Compliant.
- **[CQRS]** ✅ Compliant.

---

## Queries (Dapper Audits)
*(Currently all listed Dapper queries correctly include `[TENANT]` isolation via `@WorkspaceId`. The `[CACHE]` strategy needs a broader review to ensure all reads are correctly populated.)*
- `GetWorkspaceListHandler.cs`
- `GetMembersHandler.cs`
- `GetNodeTasksHandler.cs`
- `GetNodeSpacesHandler.cs`
- `GetNodeFoldersHandler.cs`
- `GetSpaceDocumentsHandler.cs`
- `GetFolderDetailHandler.cs`
- `GetFolderTasksHandler.cs`
- `GetTaskDetailHandler.cs`
- `GetCommentsHandler.cs`
- `GetWorkspaceWorkflowsHandler.cs`
- `GetEntityAccessHandler.cs`
- `GetDocumentBlocksHandler.cs`
