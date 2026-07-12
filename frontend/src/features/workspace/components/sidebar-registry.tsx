import { type ContentPage } from "../type";
import { HierarchySidebar } from "../contents/hierarchy/hierarchy-sidebar";

export function SidebarRegistry({ page }: { page: ContentPage }) {
  switch (page) {
    case "projects":
    case "spaces":
    case "folders":
    case "tasks":
      return <HierarchySidebar />;
    default:
      return null;
  }
}
