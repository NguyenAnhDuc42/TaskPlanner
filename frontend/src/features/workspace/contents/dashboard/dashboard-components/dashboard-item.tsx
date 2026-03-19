import { Layout, Star, MoreHorizontal } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { Route } from "@/routes/workspaces/$workspaceId";
import type { DashboardDto } from "../dashboard-type";


export function DashboardItem({ dashboard }: { dashboard: DashboardDto }) {
  const navigate = Route.useNavigate();
  const search = Route.useSearch();
  const isActive = search.dashboardId === dashboard.id;

  return (
    <div
      className={cn(
        "flex items-center group/item w-full rounded-md transition-all duration-200 pr-1 pl-2 border border-transparent overflow-hidden cursor-pointer",
        isActive 
          ? "bg-accent/40 border-current" 
          : "hover:bg-accent/10 hover:border-current"
      )}
      onClick={() => navigate({ search: { dashboardId: dashboard.id } })}
    >
      <div className="flex-1 flex items-center gap-2 py-2 min-w-0 overflow-hidden">
        <Layout className="h-4 w-4 flex-shrink-0 text-muted-foreground" />
        <span className="truncate text-sm font-medium text-foreground flex-1 min-w-0">
          {dashboard.name}
        </span>
        {dashboard.isMain && (
          <Star className="h-3 w-3 fill-yellow-500 text-yellow-500 flex-shrink-0" />
        )}
      </div>

      <div className="flex-shrink-0 opacity-0 group-hover/item:opacity-100 transition-opacity px-1">
        <Button variant="ghost" size="icon" className="h-6 w-6 hover:bg-muted">
          <MoreHorizontal className="h-3 w-3 text-muted-foreground" />
        </Button>
      </div>
    </div>
  );
}
