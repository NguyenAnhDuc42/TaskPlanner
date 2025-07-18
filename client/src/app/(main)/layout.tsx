import WorkspaceUrlSync from "@/components/providers/workspace-url-sync";
import { AppSidebar } from "@/components/sidebar/app-sidebar";
import { ThemeProvider } from "@/components/theme-provider";
import { ThemeToggle } from "@/components/theme-toggle";
import { SidebarInset, SidebarProvider, SidebarTrigger } from "@/components/ui/sidebar";
import type React from "react";
import { Toaster } from "sonner";

export default function MainLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <>
      <WorkspaceUrlSync />
      <ThemeProvider
        attribute="class"
        defaultTheme="dark"
        enableSystem
        disableTransitionOnChange
      >
        <SidebarProvider>
          <AppSidebar />
          <SidebarInset className="flex flex-col h-screen bg-background p-2">
            {/* Header bar with rounded top corners */}
            <header className="flex items-center justify-between h-12 px-4 bg-card border border-border rounded-t-xl shadow-sm">
              <div className="flex items-center gap-2">
                <SidebarTrigger className="h-6 w-6" />
                <div className="h-4 w-px bg-border" />
                <span className="text-sm font-medium text-muted-foreground">
                  TaskPlanner
                </span>
              </div>
              <ThemeToggle />
            </header>
            <main className="flex-1 bg-card border-x border-b border-border rounded-b-xl shadow-sm overflow-hidden max-h-full">
              <div className="h-full overflow-y-auto">{children}</div>
            </main>
          </SidebarInset>
        </SidebarProvider>
        <Toaster richColors />
      </ThemeProvider>
    </>
  );
}
