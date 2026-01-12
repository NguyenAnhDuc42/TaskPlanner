export type ContentPage =
  | "dashboard"
  | "tasks"
  | "calendar"
  | "members"
  | "settings";

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
}
