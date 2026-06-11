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
- **[REALTIME]** ✅ Broadcasts `EntityBatchUpdate`.
- **[PERM]** ✅ Uses `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### TransferOwnershipHandler.cs (Command)
- **[REALTIME]** ✅ Broadcasts `EntityBatchUpdate`.
- **[PERM]** ✅ Uses `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### AddMembersHandler.cs (Command)
- **[REALTIME]** ✅ Broadcasts `EntityBatchUpdate`.
- **[PERM]** ✅ Uses `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### RemoveMembersHandler.cs (Command)
- **[REALTIME]** ✅ Broadcasts `EntityBatchDelete`.
- **[PERM]** ✅ Uses `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### UpdateMembersHandler.cs (Command)
- **[REALTIME]** ✅ Broadcasts `EntityBatchUpdate`.
- **[PERM]** ✅ Uses `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.
✅ Resolved: Changed userId to memberId in frontend API and payload.

//legacy but still might be viable for future
### MoveItemHandler.cs (Command)
- **[REALTIME]** ❌ Uses `NotifyWorkspaceAsync`.
- **[PERM]** ❌ Manual `CurrentMember.Role` check.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### BatchMoveItemHandler.cs (Command)
- **[REALTIME]** ✅ Uses `EntityBatch`.
- **[PERM]** ✅ Uses `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### JoinWorkspaceByCodeHandler.cs (Command)
- **[REALTIME]** ✅ Broadcasts `EntityBatchUpdate`.
- **[PERM]** ✅ Bypasses `PermissionService` (Boundary).
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.
==> different flow cause this outside of workspace bound

### LeaveWorkspaceHandler.cs (Command)
- **[REALTIME]** ✅ Broadcasts `EntityBatchUpdate`.
- **[PERM]** ✅ Bypasses `PermissionService` (Boundary).
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.


### SetWorkspacePinHandler.cs (Command)
- **[REALTIME]** ✅ Emits user-specific pin event.
- **[PERM]** ✅ Bypasses `PermissionService` (Boundary).
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.
==> different flow cause this outside of workspace bound

### DeleteWorkspaceHandler.cs (Command)
- **[REALTIME]** ✅ Broadcasts `EntityBatchDelete`.
- **[PERM]** ✅ Bypasses `PermissionService` (Boundary).
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### CreateWorkspaceHandler.cs (Command)
- **[REALTIME]** ✅ Broadcasts `EntityBatchUpdate`.
- **[PERM]** ✅ Bypasses `PermissionService` (Boundary).
- **[CQRS]** ❌ Returns `Result<Guid>`. need to return id for quick re navigate
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.
==> different flow cause this outside of workspace bound


--- ==> check api.ts,members-api.ts to check for correct type and type safe

## Space Features

### CreateSpaceHandler.cs (Command)
- **[REALTIME]** ✅ Broadcasts `EntityBatchUpdate`.
- **[PERM]** ✅ Uses `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.
==> worksapce bound but havent reach space yet so cant check entityacces

### UpdateSpaceHandler.cs (Command)
- **[REALTIME]** ✅ Broadcasts `EntityBatchUpdate`.
- **[PERM]** ✅ Uses `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### DeleteSpaceHandler.cs (Command)
- **[REALTIME]** ✅ Broadcasts `EntityBatchDelete`.
- **[PERM]** ✅ Uses `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### BatchUpdateItemsHandler.cs (Command)
- **[REALTIME]** ✅ Broadcasts `EntityBatchUpdate`.
- **[PERM]** ✅ Uses `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.
==> added Spaceid for perm purpose need frontend update to catch up with new API
==> future will fix to refetch instead of relly on memory for record

### CreateSpaceDocumentHandler.cs (Command)
- **[REALTIME]** ⏭️ Skipped (Syncing only document blocks per user request).
- **[PERM]** ✅ Uses `PermissionService`.
- **[CQRS]** ⏭️ Kept returning data for frontend sync.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.
==> wtf is this flow 
Summarize Space Checkngi need update to be like what is in BatchUpdate
--- ==> check hierarchy-api.ts,space-api.ts to check for correct type and type safe


## Folder Features

### CreateFolderHandler.cs (Command)
- **[REALTIME]** ✅ Broadcasts `EntityBatchUpdate`.
- **[PERM]** ✅ Uses `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### UpdateFolderHandler.cs (Command)
- **[REALTIME]** ✅ Broadcasts `EntityBatchUpdate`.
- **[PERM]** ✅ Uses `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### DeleteFolderHandler.cs (Command)
- **[REALTIME]** ✅ Broadcasts `EntityBatchDelete`.
- **[PERM]** ✅ Uses `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### BatchUpdateFolderTasksHandler.cs (Command)
- **[REALTIME]** ✅ Broadcasts `EntityBatchUpdate`.
- **[PERM]** ✅ Uses `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

--- ==> check hierarchy-api.ts,folder-api.ts to check for correct type and type safe

## Task Features

### CreateTaskHandler.cs (Command)
- **[REALTIME]** ✅ Broadcasts `EntityBatchUpdate`.
- **[PERM]** ✅ Uses `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### UpdateTaskHandler.cs (Command)
- **[REALTIME]** ✅ Broadcasts `EntityBatchUpdate`.
- **[PERM]** ✅ Uses `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### DeleteTaskHandler.cs (Command)
- **[REALTIME]** ✅ Broadcasts `EntityBatchDelete`.
- **[PERM]** ✅ Uses `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### UpdateTaskAssigneesHandler.cs (Command)
- **[REALTIME]** ✅ Uses `EntityBatch`.
- **[PERM]** ✅ Uses `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### AddCommentHandler.cs (Command)
- **[REALTIME]** ✅ Broadcasts `EntityBatchUpdate`.
- **[PERM]** ✅ Uses `PermissionService`.
- **[CQRS]** ⏭️ Kept returning data for frontend sync.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### GetTaskAssigneesHandler.cs (Query)
- **[PERF]** ✅ Uses `FirstOrDefaultAsync`.

### CreateSubTaskHandler.cs (Command)
- **[REALTIME]** ✅ Uses `EntityBatchUpdate`.
- **[PERM]** ✅ Uses `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
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

--- ✅ Resolved: PermissionService creator edge case fixed. Redundant ProjectWorkspaceId checks removed from all backend feature handlers.

## Workflow Features

### UpdateWorkflowStatusesHandler.cs (Command)
- **[REALTIME]** ⏭️ Skipped per user request.
- **[PERM]** ⏭️ Skipped per user request.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### ReorderStatusesHandler.cs (Command)
- **[REALTIME]** ⏭️ Skipped per user request.
- **[PERM]** ⏭️ Skipped per user request.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### SetLayerWorkflowHandler.cs (Command)
- **[REALTIME]** ⏭️ Skipped per user request.
- **[PERM]** ⏭️ Skipped per user request.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

---

## Entity Access Features

### EntitAccessBatchHandler.cs (Command)
- **[REALTIME]** ✅ Broadcasts `EntityBatchUpdate`.
- **[PERM]** ✅ Uses `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

---

## Document Features

### UpdateDocumentBlocksHandler.cs (Command)
- **[REALTIME]** ✅ Broadcasts `EntityBatchUpdate`.
- **[PERM]** ✅ Uses `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

---

## Attachment Features

### UploadAttachmentHandler.cs (Command)
- **[REALTIME]** ✅ Broadcasts `EntityBatchUpdate`.
- **[PERM]** ✅ Uses `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### LinkAttachmentHandler.cs (Command)
- **[REALTIME]** ✅ Broadcasts `EntityBatchUpdate`.
- **[PERM]** ✅ Uses `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

### DeleteAttachmentHandler.cs (Command)
- **[REALTIME]** ✅ Broadcasts `EntityBatchDelete`.
- **[PERM]** ✅ Uses `PermissionService`.
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Optimized EF Core query.
- **[CACHE]** ⚠️ Needs verification.

---

## Auth Features
*(Auth features manage external boundaries. Realtime sync is mostly N/A)*

### ChangePasswordHandler.cs (Command)
- **[PERM]** N/A
- **[CQRS]** ✅ Returns `Result`.
- **[OPTIMIZE]** ✅ Uses `FirstOrDefaultAsync`.

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
