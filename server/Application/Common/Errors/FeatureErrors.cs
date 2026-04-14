using Application.Common.Errors;

namespace Application.Common.Errors;

public static class UserError
{
    public static readonly Error NotFound = Error.NotFound("User.NotFound", "The user with the specified identifier was not found.");
    public static readonly Error DuplicateEmail = Error.Conflict("User.DuplicateEmail", "The specified email is already in use.");
}

public static class WorkspaceError
{
    public static readonly Error NotFound = Error.NotFound("Workspace.NotFound", "The workspace with the specified identifier was not found.");
    public static readonly Error MemberAlreadyExists = Error.Conflict("Workspace.MemberAlreadyExists", "The user is already a member of this workspace.");
}

public static class SpaceError
{
    public static readonly Error NotFound = Error.NotFound("Space.NotFound", "The space with the specified identifier was not found.");
}

public static class FolderError
{
    public static readonly Error NotFound = Error.NotFound("Folder.NotFound", "The folder with the specified identifier was not found.");
    public static readonly Error HasActiveTasks = Error.Failure("Folder.HasActiveTasks", "Cannot delete folder that contains active tasks. Archive or move the tasks first.");
}

public static class TaskError
{
    public static readonly Error NotFound = Error.NotFound("Task.NotFound", "The task with the specified identifier was not found.");
}

public static class AuthError
{
    public static readonly Error InvalidCredentials = Error.Unauthorized("Auth.InvalidCredentials", "The email or password provided is incorrect.");
}

public static class ViewError
{
    public static readonly Error NotFound = Error.NotFound("View.NotFound", "The view with the specified identifier was not found.");
}

public static class MemberError
{
    public static readonly Error DontHavePermission = Error.Forbidden("Member.DontHavePermission", "You do not have permission to perform this action.");
    public static readonly Error NotFound = Error.NotFound("Member.NotFound", "The member with the specified identifier was not found.");
}

public static class ChatRoomError
{
    public static readonly Error NotFound = Error.NotFound("ChatRoom.NotFound", "The chat room with the specified identifier was not found.");
    public static readonly Error MemberAlreadyInChatRoom = Error.Conflict("ChatRoom.MemberAlreadyInChatRoom", "One or more members are already in this chat room.");
    public static readonly Error NoValidMembersToInvite = Error.Failure("ChatRoom.NoValidMembersToInvite", "There are no valid members to invite to this chat room.");
}

public static class AttachmentError
{
    public static readonly Error NotFound = Error.NotFound("Attachment.NotFound", "The attachment with the specified identifier was not found.");
    public static readonly Error UploadFailed = Error.Failure("Attachment.UploadFailed", "Failed to upload attachment.");
}
public static class WorkflowError
{
    public static readonly Error NotFound = Error.NotFound("Workflow.NotFound", "The workflow with the specified identifier was not found.");
    public static readonly Error IntegrityViolation = Error.Failure("Workflow.IntegrityViolation", "The workflow must have at least one status in each completion category.");
}
