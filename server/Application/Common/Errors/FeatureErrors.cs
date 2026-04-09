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
}

public static class TaskError
{
    public static readonly Error NotFound = Error.NotFound("Task.NotFound", "The task with the specified identifier was not found.");
}
