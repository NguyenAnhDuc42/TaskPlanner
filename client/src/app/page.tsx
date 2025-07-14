
import { AppSidebar } from "@/components/sidebar/app-sidebar"
import { ThemeToggle } from "@/components/theme-toggle"
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "@/components/ui/breadcrumb"
import { Separator } from "@/components/ui/separator"
import { SidebarInset, SidebarProvider, SidebarTrigger } from "@/components/ui/sidebar"



export default function Page() {
  return (
    <SidebarProvider>
      <AppSidebar />
      <SidebarInset className="relative">
        {/* Content layer that hovers above the sidebar layer with enhanced shadows */}
        <div
          className="absolute inset-2 z-10 flex flex-col overflow-hidden rounded-2xl bg-background transition-all duration-300 ease-out peer-data-[state=collapsed]:inset-x-2 peer-data-[state=collapsed]:left-6 md:peer-data-[state=collapsed]:left-8
                        shadow-[0_0_0_1px_rgba(0,0,0,0.05),0_2px_4px_rgba(0,0,0,0.1),0_8px_16px_rgba(0,0,0,0.1),0_16px_32px_rgba(0,0,0,0.1)]
                        dark:shadow-[0_0_0_1px_rgba(255,255,255,0.1),0_2px_4px_rgba(0,0,0,0.3),0_8px_16px_rgba(0,0,0,0.3),0_16px_32px_rgba(0,0,0,0.3)]"
        >
          {/* Multiple shadow layers for enhanced depth */}
          <div className="absolute -inset-1 rounded-2xl bg-gradient-to-r from-black/5 via-transparent to-black/5 blur-sm dark:from-white/5 dark:to-white/5"></div>
          <div className="absolute -inset-2 rounded-2xl bg-gradient-to-r from-black/3 via-transparent to-black/3 blur-md dark:from-white/3 dark:to-white/3"></div>

          {/* Compact Header with glass effect */}
          <header className="relative z-20 flex h-12 shrink-0 items-center gap-2 border-b bg-background/60 backdrop-blur-md px-4">
            <SidebarTrigger className="-ml-1 h-6 w-6" />
            <Separator orientation="vertical" className="mr-2 h-3" />
            <Breadcrumb>
              <BreadcrumbList className="text-xs">
                <BreadcrumbItem className="hidden md:block">
                  <BreadcrumbLink href="#" className="transition-colors hover:text-foreground text-xs">
                    Workspace
                  </BreadcrumbLink>
                </BreadcrumbItem>
                <BreadcrumbSeparator className="hidden md:block" />
                <BreadcrumbItem>
                  <BreadcrumbPage className="text-xs">Tasks</BreadcrumbPage>
                </BreadcrumbItem>
              </BreadcrumbList>
            </Breadcrumb>
            <div className="ml-auto">
              <ThemeToggle />
            </div>
          </header>

       
        </div>
      </SidebarInset>
    </SidebarProvider>
  )
}
