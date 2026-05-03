import { Home, Folder, FileText, type LucideIcon } from "lucide-react";

export type WorkspaceTheme = "Light" | "Dark" | "System" | "Mars" | "DeepSpace" | "Boreal";

export type ContentPage =
  | "projects"
  | "spaces"
  | "folders"
  | "tasks"
  | "calendar"
  | "members"
  | "communications"
  | "settings"
  | "command-center";

export interface SidebarContextType {
  isInnerSidebarOpen: boolean;
  setIsInnerSidebarOpen: (open: boolean) => void;
  toggleInnerSidebar: () => void;
  activeContent: ContentPage;
  setActiveContent: (content: ContentPage) => void;
  isHovering: boolean;
  setIsHovering: (hovering: boolean) => void;
  hoveredIcon: ContentPage | null;
  setHoveredIcon: (icon: ContentPage | null) => void;
  workspaceId: string | null;
  setWorkspaceId: (id: string | null) => void;
}

export interface NavItem {
  id: string;
  icon: LucideIcon;
  label: string;
}

export const getNavigationItems = (contentType: ContentPage): NavItem[] => {
  switch (contentType) {
    case "command-center":
    case "projects":
      return [
        { id: "all-tasks", icon: FileText, label: "All Tasks" },
        { id: "my-tasks", icon: Home, label: "My Tasks" },
        { id: "completed", icon: Folder, label: "Completed" },
      ];
    case "members":
      return [
        { id: "all-members", icon: Home, label: "All Members" },
        { id: "teams", icon: Folder, label: "Teams" },
        { id: "roles", icon: FileText, label: "Roles" },
      ];
    case "calendar":
      return [
        { id: "month", icon: Home, label: "Month View" },
        { id: "week", icon: Folder, label: "Week View" },
        { id: "day", icon: FileText, label: "Day View" },
      ];
    case "settings":
      return [
        { id: "general", icon: Home, label: "General" },
        { id: "account", icon: Folder, label: "Account" },
        { id: "privacy", icon: FileText, label: "Privacy" },
      ];
    default:
      return [
        { id: "item-1", icon: Home, label: "Item 1" },
        { id: "item-2", icon: Folder, label: "Item 2" },
      ];
  }
};
