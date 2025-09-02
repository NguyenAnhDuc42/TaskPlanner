namespace Domain.Enums
{
    [Flags]
    public enum Permission : long
    {
        None = 0,
        
        // Workspace Permissions
        View_Workspace = 1L << 0,
        Edit_Workspace = 1L << 1,
        Delete_Workspace = 1L << 2,
        Archive_Workspace = 1L << 3,
        Manage_Workspace_Settings = 1L << 4,
        Transfer_Ownership = 1L << 5,
        
        // Member Permissions  
        View_Members = 1L << 6,
        Invite_Members = 1L << 7,
        Remove_Members = 1L << 8,
        Manage_Member_Roles = 1L << 9,
        
        // Space Permissions
        View_Spaces = 1L << 10,
        Create_Spaces = 1L << 11,
        Edit_Spaces = 1L << 12,
        Delete_Spaces = 1L << 13,
        Archive_Spaces = 1L << 14,
        
        // List Permissions
        View_Lists = 1L << 15,
        Create_Lists = 1L << 16,
        Edit_Lists = 1L << 17,
        Delete_Lists = 1L << 18,
        Reorder_Lists = 1L << 19,
        
        // Task Permissions
        View_Tasks = 1L << 20,
        Create_Tasks = 1L << 21,
        Edit_Tasks = 1L << 22,
        Delete_Tasks = 1L << 23,
        Assign_Tasks = 1L << 24,
        Change_Task_Status = 1L << 25,
        
        // Status Permissions
        View_Statuses = 1L << 26,
        Create_Statuses = 1L << 27,
        Edit_Statuses = 1L << 28,
        Delete_Statuses = 1L << 29,
        Reorder_Statuses = 1L << 30,
        
        // Comment Permissions
        View_Comments = 1L << 31,
        Create_Comments = 1L << 32,
        Edit_Own_Comments = 1L << 33,
        Edit_All_Comments = 1L << 34,
        Delete_Own_Comments = 1L << 35,
        Delete_All_Comments = 1L << 36,
        
        // File/Attachment Permissions
        View_Attachments = 1L << 37,
        Upload_Attachments = 1L << 38,
        Delete_Own_Attachments = 1L << 39,
        Delete_All_Attachments = 1L << 40,
        
        // Reporting Permissions
        View_Reports = 1L << 41,
        Export_Data = 1L << 42,
        
        // Admin Permissions (shortcuts for common combinations)
        Workspace_Admin = View_Workspace | Edit_Workspace | Archive_Workspace | Manage_Workspace_Settings,
        Member_Admin = View_Members | Invite_Members | Remove_Members | Manage_Member_Roles,
        Content_Admin = View_Spaces | Create_Spaces | Edit_Spaces | Delete_Spaces | 
                       View_Lists | Create_Lists | Edit_Lists | Delete_Lists |
                       View_Tasks | Create_Tasks | Edit_Tasks | Delete_Tasks,
        
        // Owner has everything except transfer ownership (business rule)
        Owner_Permissions = ~Transfer_Ownership,
        
        // Full access
        All = ~None
    }
}