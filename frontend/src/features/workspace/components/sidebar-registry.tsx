import { type ContentPage, getNavigationItems } from "../type";
import { Button } from "@/components/ui/button";
import { MembersSidebar } from "../contents/members/member-components/members-sidebar";

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
            className="w-full justify-start gap-3 px-3 py-2 h-10 transition-colors hover:bg-accent/50"
          >
            <Icon className="h-4 w-4" />
            <span className="text-sm font-medium">{item.label}</span>
          </Button>
        );
      })}
    </div>
  );
};

// THE REGISTRY
export function SidebarRegistry({ page }: { page: ContentPage }) {
  switch (page) {
    case "members":
      return <MembersSidebar />;
    // Add more feature sidebars here
    default:
      return <DefaultSidebar page={page} />;
  }
}
