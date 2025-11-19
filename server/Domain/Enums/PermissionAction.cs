namespace Domain.Enums;

public enum PermissionAction
{

    View,
    Create,
    Edit,        // prefer 'Update' over 'Edit' for clarity
    Delete,
    Archive,
    ManageSettings,
    InviteMember,
    RemoveMember,
    UpdateMember,
    TransferOwnership,
    Comment,
    UploadAttachment,
    Assign,        // assign tasks
    ChangeStatus,   // move task state

}
