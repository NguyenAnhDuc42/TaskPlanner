import { type ContentPage, getNavigationItems } from "../type";
import { Button } from "@/components/ui/button";
import { MembersSidebar } from "../contents/members/member-components/members-sidebar";
import { SettingsSidebar } from "../contents/setting/setting-components/settings-sidebar";
import { HierarchySidebar } from "../contents/hierarchy/hierarchy-sidebar";
import { ScrollArea } from "@/components/ui/scroll-area";

// FEATURE SIDEBARS

const DefaultSidebar = ({ page }: { page: ContentPage }) => {
  const navItems = getNavigationItems(page);
  return (
    <ScrollArea className="flex-1 min-h-0">
      <div className="space-y-1 animate-in fade-in slide-in-from-left-1 duration-200">
        {navItems.map((item) => {
          const Icon = item.icon;
          return (
            <Button
              key={item.id}
              variant="ghost"
              className="w-full justify-start gap-3 px-3 h-9 text-muted-foreground hover:text-foreground hover:bg-muted transition-colors"
            >
              <Icon className="h-4 w-4 flex-shrink-0" />
              <span className="text-sm font-medium truncate">{item.label}</span>
            </Button>
          );
        })}
      </div>
    </ScrollArea>
  );
};

// THE REGISTRY
export function SidebarRegistry({ page }: { page: ContentPage }) {
  switch (page) {
    case "members":
    case "communications":
      return <MembersSidebar />;
    case "settings":
      return <SettingsSidebar />;
    case "command-center":
    case "projects":
      return <HierarchySidebar />;
    case "spaces":
      return <HierarchySidebar />;
    case "folders":
      return <HierarchySidebar />;
    case "tasks":
      return <HierarchySidebar />;
    default:
      return <DefaultSidebar page={page as ContentPage} />;
  }
}
