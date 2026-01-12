import { SidebarProvider } from "./components/sidebar-provider";
import { OuterSidebar } from "./components/outer-sidebar";
import { ContentDisplayer } from "./components/content-displayer";

export default function WorkspacePage() {
  return (
    <SidebarProvider defaultContent="dashboard" defaultOpen={true}>
      <div className="flex h-screen w-full overflow-hidden bg-background p-2 gap-4">
        {/* Outer Sidebar - Floating design */}
        <OuterSidebar />

        {/* Content Displayer - Floating design */}
        <ContentDisplayer />
      </div>
    </SidebarProvider>
  );
}
