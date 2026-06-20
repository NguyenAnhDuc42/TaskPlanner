import { type ContentPage } from "../type";
import { SettingsSidebar } from "../contents/setting/setting-components/settings-sidebar";
import { HierarchySidebar } from "../contents/hierarchy/hierarchy-sidebar";

export function SidebarRegistry({ page }: { page: ContentPage }) {
  switch (page) {
    case "settings":
      return <SettingsSidebar />;
    case "command-center":
    case "projects":
    case "spaces":
    case "folders":
    case "tasks":
      return <HierarchySidebar />;
    default:
      return null;
  }
}
