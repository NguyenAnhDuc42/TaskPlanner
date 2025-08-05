import WorkspaceUrlSync from "@/components/providers/workspace-url-sync";
import { SidebarInset, SidebarProvider } from "@/components/ui/sidebar";
import type React from "react";
import { Toaster } from "sonner";
import { AppSidebar } from "./(component)/sidebar/sidebar";

export default function WorkspaceLayout({ children,}: Readonly<{ children: React.ReactNode;}>) {
  return (
    <>
      <WorkspaceUrlSync />
           <SidebarProvider>
              <AppSidebar />
              <SidebarInset className="relative">
                  <div className="min-h-screen w-full">
                    <div className="absolute top-6 left-6 right-6 bottom-6">
                      <div className="bg-card border border-border rounded-2xl shadow-lg h-full p-8 overflow-auto">
                        {children}
                      </div>
                    </div>
                  </div>
          </SidebarInset>
        </SidebarProvider>
        <Toaster richColors />
    </>
  );
}
