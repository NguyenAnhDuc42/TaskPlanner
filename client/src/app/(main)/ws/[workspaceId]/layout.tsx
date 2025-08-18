import WorkspaceUrlSync from "@/components/providers/workspace-url-sync";
import { SidebarInset, SidebarProvider } from "@/components/ui/sidebar";
import type React from "react";
import { Toaster } from "sonner";
import { WorkspaceSidebar } from "./(component)/sidebar/workspace-sidebar";
import WorkspaceHeaderBar from "./(component)/workspace-header-bar";

export default function WorkspaceLayout({ children,}: Readonly<{ children: React.ReactNode;}>) {
  return (
    <>
      <WorkspaceUrlSync />
           <SidebarProvider>
              <WorkspaceSidebar />
              <SidebarInset className="relative">
                  <div className="min-h-screen w-full">
                    <div className="absolute top-6 left-6 right-6 bottom-6">
                      <div className=" bg-background border border-border rounded-2xl shadow-lg h-full overflow-hidden">
                        <WorkspaceHeaderBar />
                        <div className="h-full w-full p-4 overflow-auto">
                         {children}
                        </div>
                      </div>
                    </div>
                  </div>
          </SidebarInset>
        </SidebarProvider>
        <Toaster richColors />
    </>
  );
}
