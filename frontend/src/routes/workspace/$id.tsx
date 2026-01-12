import { createFileRoute } from "@tanstack/react-router";
import { SidebarProvider } from "@/features/workspace/components/sidebar-provider";
import { OuterSidebar } from "@/features/workspace/components/outer-sidebar";
import { ContentDisplayer } from "@/features/workspace/components/content-displayer";

export const Route = createFileRoute("/workspace/$id")({
  component: WorkspaceLayout,
});

function WorkspaceLayout() {
  const { id } = Route.useParams();

  return (
    <SidebarProvider initialWorkspaceId={id}>
      <div className="flex h-screen w-full overflow-hidden bg-background p-2 gap-4">
        {/* Outer Sidebar - Persistent Visual Frame */}
        <OuterSidebar />

        {/* Content Displayer - Persistent Visual Frame with Inner Sidebar */}
        <ContentDisplayer />
      </div>
    </SidebarProvider>
  );
}
