namespace Api;

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
    public static readonly Error InvalidSession = Error.Unauthorized("Auth.InvalidSession", "Invalid or expired session.");
    public static readonly Error OAuthOnlyAccount = Error.Unauthorized("Auth.OAuthOnlyAccount", "This account signs in with Google or GitHub — use that instead of a password.");
}


public static class MemberError
{
    public static readonly Error DontHavePermission = Error.Forbidden("Member.DontHavePermission", "You do not have permission to perform this action.");
    public static readonly Error NotFound = Error.NotFound("Member.NotFound", "The member with the specified identifier was not found.");
}
public static class FavoriteError
{
    public static readonly Error NotFound = Error.NotFound("Favorite.NotFound", "The favorite item with the specified identifier was not found.");
}
public static class AttachmentError
{
    public static readonly Error NotFound = Error.NotFound("Attachment.NotFound", "The attachment with the specified identifier was not found.");
    public static readonly Error UploadFailed = Error.Failure("Attachment.UploadFailed", "Failed to upload attachment.");
}

public static class CommonError
{
     public static readonly Error DatabaseError = Error.Failure("Common.DatabaseError",  "An unexpected database error occurred.");
}

public static class CommentError
{
    public static readonly Error NotFound = Error.NotFound("Comment.NotFound", "The comment with the specified identifier was not found.");
}

public static class DocumentBlockError
{
    public static readonly Error NotFound = Error.NotFound("DocumentBlock.NotFound", "The document block with the specified identifier was not found.");
}

public static class EntityAccessError
{
    public static readonly Error NotFound = Error.NotFound("Access.NotFound", "One or more of the specified access rows were not found.");
    public static readonly Error InvalidMember = Error.Validation("Member.Invalid", "One or more of the specified members are not part of this workspace.");
}

public static class AssigneeError
{
    public static readonly Error NotFound = Error.NotFound("Assignee.NotFound", "The assignee with the specified identifier was not found.");
}


