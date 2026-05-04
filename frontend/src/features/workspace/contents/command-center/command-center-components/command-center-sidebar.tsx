import { Button } from "@/components/ui/button";
import { Link, useParams } from "@tanstack/react-router";
import { LayoutDashboard, Target, Zap, Activity } from "lucide-react";
import { ScrollArea } from "@/components/ui/scroll-area";

export function CommandCenterSidebar() {
  const { workspaceId } = useParams({ from: "/workspaces/$workspaceId" });

  const navItems = [
    { id: "", icon: LayoutDashboard, label: "Overview" },
    { id: "analytics", icon: Activity, label: "Analytics" },
    { id: "performance", icon: Zap, label: "Performance" },
    { id: "goals", icon: Target, label: "Strategic Goals" },
  ];

  return (
    <ScrollArea className="flex-1 min-h-0">
      <div className="space-y-4 animate-in fade-in slide-in-from-left-1 duration-200">
        <div className="px-1 py-2">
          <h3 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-2">
            Control Panel
          </h3>
          <div className="space-y-1">
            {navItems.map((item) => {
              const Icon = item.icon;
              return (
                <Button
                  key={item.id}
                  variant="ghost"
                  className="w-full justify-start gap-3 h-10 px-3 hover:bg-accent/50 group"
                  asChild
                >
                  <Link to={"/workspaces/" + workspaceId + (item.id ? "/" + item.id : "")}>
                    <Icon className="h-4 w-4 text-muted-foreground group-hover:text-primary transition-colors" />
                    <span className="text-sm font-medium">{item.label}</span>
                  </Link>
                </Button>
              );
            })}
          </div>
        </div>
      </div>
    </ScrollArea>
  );
}
