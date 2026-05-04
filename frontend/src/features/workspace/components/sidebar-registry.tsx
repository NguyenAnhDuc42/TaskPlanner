import { type ContentPage } from "../type";
import { MembersSidebar } from "../contents/members/member-components/members-sidebar";
import { SettingsSidebar } from "../contents/setting/setting-components/settings-sidebar";
import { HierarchySidebar } from "../contents/hierarchy/hierarchy-sidebar";
import { CommandCenterSidebar } from "../contents/command-center/command-center-components/command-center-sidebar";

// THE REGISTRY
export function SidebarRegistry({ page }: { page: ContentPage }) {
  switch (page) {
    case "members":
      return <MembersSidebar />;
    case "settings":
      return <SettingsSidebar />;
    case "command-center":
      return <CommandCenterSidebar />;
    case "projects":
    case "spaces":
    case "folders":
    case "tasks":
      return <HierarchySidebar />;
    default:
      return null;
  }
}
