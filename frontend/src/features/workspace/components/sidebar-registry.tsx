import { type ContentPage, getNavigationItems } from "../type";
import { Button } from "@/components/ui/button";
import { MembersSidebar } from "../contents/members/member-components/members-sidebar";
import { SettingsSidebar } from "../contents/setting/setting-components/settings-sidebar";
import { HierarchySidebar } from "../contents/hierarchy/hierarchy-sidebar";
import { DashboardSidebar } from "../contents/dashboard/dashboard-sidebar";

// FEATURE SIDEBARS

const DefaultSidebar = ({ page }: { page: ContentPage }) => {
  const navItems = getNavigationItems(page);
  return (
    <div className="space-y-1 animate-in fade-in slide-in-from-left-1 duration-200">
      {navItems.map((item) => {
        const Icon = item.icon;
        return (
          <Button
            key={item.id}
            variant="ghost"
            className="w-full justify-start gap-3 px-3 py-2 h-10 transition-all duration-200 hover:bg-[var(--theme-item-hover)] group"
          >
            <Icon className="h-4 w-4 text-[var(--theme-text-normal)] group-hover:text-[var(--theme-text-hover)] transition-colors" />
            <span className="text-sm font-medium text-[var(--theme-text-normal)] group-hover:text-[var(--theme-text-hover)] transition-colors">
              {item.label}
            </span>
          </Button>
        );
      })}
    </div>
  );
};

// THE REGISTRY
export function SidebarRegistry({ page }: { page: ContentPage }) {
  switch (page) {
    case "dashboard":
      return <DashboardSidebar />;
    case "members":
    case "communications":
      return <MembersSidebar />;
    case "settings":
      return <SettingsSidebar />;
    case "projects":
      return <HierarchySidebar />;
    case "spaces":
    case "folders":
    case "lists":
      return <HierarchySidebar />;
    default:
      return <DefaultSidebar page={page as ContentPage} />;
  }
}
