import { createFileRoute } from "@tanstack/react-router";
import { SidebarProvider } from "@/features/workspace/components/sidebar-provider";
import { OuterSidebar } from "@/features/workspace/components/outer-sidebar";
import { InnerSidebar } from "@/features/workspace/components/inner-sidebar";
import { ContentDisplayer } from "@/features/workspace/components/content-displayer";
import { z } from "zod";

export const workspaceSearchSchema = z.object({
  dashboardId: z.uuid().optional(),
});

export const Route = createFileRoute("/workspaces/$workspaceId")({
  validateSearch: (search) => workspaceSearchSchema.parse(search),
  component: WorkspaceLayout,
});

function WorkspaceLayout() {
  const { workspaceId } = Route.useParams();

  return (
    <SidebarProvider initialWorkspaceId={workspaceId}>
      <div className="flex h-screen w-full overflow-hidden bg-background p-4 gap-4">
        {/* Column 1: Outer Sidebar */}
        <OuterSidebar />

        {/* Column 2: Contextual Inner Sidebar */}
        <InnerSidebar />

        {/* Column 3: The Main Canvas */}
        <ContentDisplayer />
      </div>
    </SidebarProvider>
  );
}
