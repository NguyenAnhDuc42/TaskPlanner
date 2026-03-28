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
          ? "theme-selected shadow-md" 
          : "hover:bg-[var(--theme-item-hover)] group"
      )}
      onClick={() => navigate({ search: { dashboardId: dashboard.id } })}
    >
      <div className="flex-1 flex items-center gap-2 py-2 min-w-0 overflow-hidden">
        <Layout className={cn(
          "h-4 w-4 flex-shrink-0 transition-colors",
          isActive ? "text-[var(--theme-text-active)]" : "text-[var(--theme-text-normal)] group-hover/item:text-[var(--theme-text-hover)]"
        )} />
        <span className={cn(
          "truncate text-sm font-medium flex-1 min-w-0 transition-colors",
          isActive ? "text-[var(--theme-text-active)]" : "text-[var(--theme-text-normal)] group-hover/item:text-[var(--theme-text-hover)]"
        )}>
          {dashboard.name}
        </span>
        {dashboard.isMain && (
          <Star className={cn(
            "h-3 w-3 flex-shrink-0",
            isActive ? "fill-current text-[var(--theme-text-active)]" : "fill-yellow-500 text-yellow-500"
          )} />
        )}
      </div>

      <div className="flex-shrink-0 opacity-0 group-hover/item:opacity-100 transition-opacity px-1">
        <Button 
          variant="ghost" 
          size="icon" 
          className={cn(
            "h-6 w-6 rounded-md transition-colors",
            isActive ? "text-[var(--theme-text-active)] hover:bg-black/10" : "text-[var(--theme-text-normal)] hover:text-[var(--theme-text-hover)] hover:bg-[var(--theme-item-hover)]"
          )}
        >
          <MoreHorizontal className="h-3 w-3" />
        </Button>
      </div>
    </div>
  );
}
