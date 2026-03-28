import { createFileRoute } from "@tanstack/react-router";
import { SidebarProvider } from "@/features/workspace/components/sidebar-provider";
import { OuterSidebar } from "@/features/workspace/components/outer-sidebar";
import { InnerSidebar } from "@/features/workspace/components/inner-sidebar";
import { ContentDisplayer } from "@/features/workspace/components/content-displayer";
import { z } from "zod";
import { cn } from "@/lib/utils";

export const workspaceSearchSchema = z.object({
  dashboardId: z.uuid().optional(),
});

export const Route = createFileRoute("/workspaces/$workspaceId")({
  validateSearch: (search) => workspaceSearchSchema.parse(search),
  component: WorkspaceLayout,
});

import { useWorkspaceDetail } from "@/features/workspace/api";
import { Loader2 } from "lucide-react";
import { useSidebarContext } from "@/features/workspace/components/sidebar-provider";

function WorkspaceLayout() {
  const { workspaceId } = Route.useParams();
  const { data: workspace, isLoading } = useWorkspaceDetail(workspaceId);

  if (isLoading) {
    return (
      <div className="h-screen w-full flex items-center justify-center bg-background">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  return (
    <SidebarProvider initialWorkspaceId={workspaceId}>
      <WorkspaceThemeLayout workspace={workspace} />
    </SidebarProvider>
  );
}

function WorkspaceThemeLayout({ workspace }: { workspace: any }) {
  const { isInnerSidebarOpen } = useSidebarContext();
  const themeClass = workspace?.theme ? `theme-${workspace.theme.toLowerCase()}` : "";
  const isPremiumTheme = workspace?.theme === "Mars" || workspace?.theme === "DeepSpace" || workspace?.theme === "Boreal";

  return (
    <div className={cn(
      "relative flex h-screen w-full overflow-hidden p-4 transition-colors duration-500", 
      !isPremiumTheme && "bg-background",
      themeClass
    )}>
      {/* Background Mesh for Premium Themes */}
      {isPremiumTheme && <div className="sandy-mesh-bg" />}

      {/* Column 1: Outer Sidebar */}
      <OuterSidebar className="flex-shrink-0 mr-4" />

      {/* Column 2: Contextual Inner Sidebar */}
      <InnerSidebar className={cn("flex-shrink-0 transition-all duration-300", isInnerSidebarOpen ? "mr-4" : "mr-0")} />

      {/* Column 3: The Main Canvas */}
      <div className="flex-1 min-w-0 h-full">
        <ContentDisplayer />
      </div>
    </div>
  );
}
