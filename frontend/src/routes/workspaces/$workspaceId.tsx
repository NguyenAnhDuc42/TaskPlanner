import { createFileRoute } from "@tanstack/react-router";
import { SidebarProvider } from "@/features/workspace/components/sidebar-provider";
import { WorkspaceProvider, useWorkspace } from "@/features/workspace/context/workspace-provider";
import { OuterSidebar } from "@/features/workspace/components/outer-sidebar";
import { InnerSidebar } from "@/features/workspace/components/inner-sidebar";
import { ContentDisplayer } from "@/features/workspace/components/content-displayer";
import { z } from "zod";
import { cn } from "@/lib/utils";
import { Loader2 } from "lucide-react";
import { useSidebarContext } from "@/features/workspace/components/sidebar-provider";

export const workspaceSearchSchema = z.object({});

export const Route = createFileRoute("/workspaces/$workspaceId")({
  validateSearch: (search) => workspaceSearchSchema.parse(search),
  component: WorkspaceLayout,
});


function WorkspaceLayout() {
  const { workspaceId } = Route.useParams();

  return (
    <WorkspaceProvider workspaceId={workspaceId}>
      <WorkspaceContent />
    </WorkspaceProvider>
  );
}

function WorkspaceContent() {
  const { workspace, isLoading, workspaceId } = useWorkspace();

  if (isLoading) {
    return (
      <div className="h-screen w-full flex items-center justify-center bg-background text-primary">
        <Loader2 className="h-8 w-8 animate-spin" />
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

  return (
    <div className={cn(
      "relative flex h-screen w-full overflow-hidden p-4 transition-colors duration-500 bg-background", 
      themeClass
    )}>
      {/* Column 1: Outer Sidebar */}
      <OuterSidebar className="flex-shrink-0 mr-3" />

      {/* Column 2: Contextual Inner Sidebar */}
      <InnerSidebar className={cn("flex-shrink-0 transition-all duration-300", isInnerSidebarOpen ? "mr-3" : "mr-0")} />

      {/* Column 3: The Main Canvas */}
      <div className="flex-1 min-w-0 h-full">
        <ContentDisplayer />
      </div>
    </div>
  );
}
