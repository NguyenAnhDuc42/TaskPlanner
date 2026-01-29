namespace Domain.Enums;

public enum PermissionAction
{

    View,
    Create,
    Edit,        // prefer 'Update' over 'Edit' for clarity
    Delete,
    Archive,
    Manage,
    ManageSettings,
    TransferOwnership,
    Comment,
    UploadAttachment,
    Assign,        // assign tasks
    ChangeStatus,   // move task state

}
