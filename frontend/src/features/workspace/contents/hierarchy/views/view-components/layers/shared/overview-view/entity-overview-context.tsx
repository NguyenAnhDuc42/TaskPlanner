import { ScrollArea } from "@/components/ui/scroll-area";
import {
  FileText,
  Calendar as CalendarIcon,
  Layers,
  ChevronDown,
  Layout,
  Activity,
  CheckCircle2,
  Clock,
} from "lucide-react";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { format } from "date-fns";
import type { OverviewViewData, OverviewStatusOptionDto } from "../../../../views-type";
import { useUpdateSpace, useUpdateFolder } from "../../../../../hierarchy-api";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";

import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { Calendar } from "@/components/ui/calendar";
import { toast } from "sonner";
import { cn } from "@/lib/utils";
import { useParams } from "@tanstack/react-router";

interface EntityOverviewContextProps {
  data?: OverviewViewData | null;
  entityId: string;
  entityType: "space" | "folder";
}

export function EntityOverviewContext({ data, entityId, entityType }: EntityOverviewContextProps) {
  const { workspaceId } = useParams({ from: "/workspaces/$workspaceId" });
  const updateSpace = useUpdateSpace(workspaceId);
  const updateFolder = useUpdateFolder(workspaceId);

  if (!data) return null;

  const handleStatusUpdate = async (status: OverviewStatusOptionDto) => {
    try {
      if (entityType === "space") {
        await updateSpace.mutateAsync({ spaceId: entityId, statusId: status.id });
      } else {
        await updateFolder.mutateAsync({ folderId: entityId, statusId: status.id });
      }
      toast.success(`Status updated to ${status.name}`);
    } catch (error) {
      toast.error("Failed to update status");
    }
  };

  const handleDateUpdate = async (field: "startDate" | "dueDate", date: Date | undefined) => {
    try {
      const dateString = date?.toISOString();
      if (entityType === "space") {
        await updateSpace.mutateAsync({ spaceId: entityId, [field]: dateString });
      } else {
        await updateFolder.mutateAsync({ folderId: entityId, [field]: dateString });
      }
      toast.success(`${field === "startDate" ? "Start date" : "Due date"} updated`);
    } catch (error) {
      toast.error(`Failed to update ${field}`);
    }
  };

  const progressPercentage = data.progress.totalTasks > 0 
    ? (data.progress.completedTasks / data.progress.totalTasks) * 100 
    : 0;

  return (
    <div className="flex-1 flex flex-col h-full bg-background select-none">
      <Tabs defaultValue="general" className="flex-1 flex flex-col h-full">
        <div className="h-12 px-5 flex items-center justify-between border-b border-border/40 bg-background/50 backdrop-blur-md">
          <TabsList className="bg-transparent border-none p-0 h-full gap-3 items-center">
            <TabIconTrigger value="general" icon={Layout} />
            <TabIconTrigger value="docs" icon={FileText} />
          </TabsList>
        </div>

        <ScrollArea className="flex-1">
          <TabsContent
            value="general"
            className="m-0 animate-in fade-in slide-in-from-right-1 duration-200"
          >
            {/* PROPERTY GRID */}
            <div className="py-4 space-y-1">
              <div className="px-5 pb-2">
                <span className="text-[10px] font-bold uppercase tracking-[0.2em] text-muted-foreground/30">
                  Properties
                </span>
              </div>

              <PropertyRow icon={Layers} label="Status">
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <div className="flex items-center gap-2 cursor-pointer hover:bg-foreground/[0.05] -ml-1.5 px-1.5 py-0.5 rounded-md transition-colors group/status">
                      {data.status ? (
                        <>
                          <div 
                            className="h-2 w-2 rounded-full" 
                            style={{ backgroundColor: data.status.color }}
                          />
                          <span className="font-semibold text-foreground/80">
                            {data.status.name}
                          </span>
                        </>
                      ) : (
                        <>
                          <div className="h-2 w-2 rounded-full bg-muted-foreground opacity-40" />
                          <span className="font-semibold italic opacity-40">No status</span>
                        </>
                      )}
                      <ChevronDown className="h-3 w-3 opacity-0 group-hover/status:opacity-30 transition-opacity" />
                    </div>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="start" className="w-56 p-1 bg-background/95 backdrop-blur-md border-border/40 shadow-2xl rounded-xl">
                    <div className="px-2 py-1.5 pb-2 border-b border-border/10 mb-1">
                      <span className="text-[10px] font-black uppercase tracking-widest text-muted-foreground/40">
                        Select Status
                      </span>
                    </div>
                    {data.availableStatuses?.map((s) => (
                      <DropdownMenuItem 
                        key={s.id}
                        onClick={() => handleStatusUpdate(s)}
                        className={cn(
                          "flex items-center gap-2 px-2 py-2 rounded-lg cursor-pointer transition-colors",
                          data.status?.name === s.name ? "bg-primary/5 text-primary" : "hover:bg-foreground/[0.03]"
                        )}
                      >
                        <div className="h-2 w-2 rounded-full" style={{ backgroundColor: s.color }} />
                        <div className="flex flex-col">
                          <span className="text-xs font-bold">{s.name}</span>
                          <span className="text-[9px] opacity-40 font-black uppercase tracking-tighter">{s.category}</span>
                        </div>
                        {data.status?.name === s.name && (
                          <div className="ml-auto h-1.5 w-1.5 rounded-full bg-primary" />
                        )}
                      </DropdownMenuItem>
                    ))}
                  </DropdownMenuContent>
                </DropdownMenu>
              </PropertyRow>

              {data.workflowName && (
                <PropertyRow icon={CheckCircle2} label="Workflow">
                  <span className="font-semibold text-foreground/80">
                    {data.workflowName}
                  </span>
                </PropertyRow>
              )}

              <PropertyRow icon={CalendarIcon} label="Start Date">
                <Popover>
                  <PopoverTrigger asChild>
                    <div className="flex items-center gap-2 cursor-pointer hover:bg-foreground/[0.05] -ml-1.5 px-1.5 py-0.5 rounded-md transition-colors group/date">
                      <span className={cn(
                        "font-semibold transition-colors",
                        data.startDate ? "text-foreground/80" : "text-muted-foreground/40 italic"
                      )}>
                        {data.startDate ? format(new Date(data.startDate), "MMM d, yyyy") : "No start date"}
                      </span>
                      <ChevronDown className="h-3 w-3 opacity-0 group-hover/date:opacity-30 transition-opacity" />
                    </div>
                  </PopoverTrigger>
                  <PopoverContent className="w-auto p-0 border-border/40 shadow-2xl rounded-xl overflow-hidden" align="start">
                    <Calendar
                      mode="single"
                      selected={data.startDate ? new Date(data.startDate) : undefined}
                      onSelect={(date) => handleDateUpdate("startDate", date)}
                      initialFocus
                    />
                  </PopoverContent>
                </Popover>
              </PropertyRow>

              <PropertyRow icon={Clock} label="Due Date">
                <Popover>
                  <PopoverTrigger asChild>
                    <div className="flex items-center gap-2 cursor-pointer hover:bg-foreground/[0.05] -ml-1.5 px-1.5 py-0.5 rounded-md transition-colors group/date">
                      <span className={cn(
                        "font-semibold transition-colors",
                        data.dueDate ? "text-foreground/80" : "text-muted-foreground/40 italic"
                      )}>
                        {data.dueDate ? format(new Date(data.dueDate), "MMM d, yyyy") : "No due date"}
                      </span>
                      <ChevronDown className="h-3 w-3 opacity-0 group-hover/date:opacity-30 transition-opacity" />
                    </div>
                  </PopoverTrigger>
                  <PopoverContent className="w-auto p-0 border-border/40 shadow-2xl rounded-xl overflow-hidden" align="start">
                    <Calendar
                      mode="single"
                      selected={data.dueDate ? new Date(data.dueDate) : undefined}
                      onSelect={(date) => handleDateUpdate("dueDate", date)}
                      initialFocus
                    />
                  </PopoverContent>
                </Popover>
              </PropertyRow>
            </div>

            {/* SECTIONS */}
            <div className="mt-4 border-t border-border/40">
              <CollapsibleSection title="Progress" count={null}>
                <div className="space-y-4">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-3">
                      <div className="w-2 h-2 rounded-sm bg-muted" />
                      <span className="text-[11px] font-bold text-muted-foreground/60">
                        Total
                      </span>
                      <span className="text-[11px] font-black">{data.progress.totalTasks}</span>
                    </div>
                    <div className="flex items-center gap-3">
                      <div className="w-2 h-2 rounded-sm bg-primary" />
                      <span className="text-[11px] font-bold text-muted-foreground/60">
                        Done
                      </span>
                      <span className="text-[11px] font-black text-primary">
                        {data.progress.completedTasks}
                      </span>
                    </div>
                  </div>
                  <div className="h-1.5 w-full bg-muted rounded-full overflow-hidden">
                    <div 
                      className="h-full bg-primary transition-all duration-500" 
                      style={{ width: `${progressPercentage}%` }}
                    />
                  </div>
                </div>
              </CollapsibleSection>

              <CollapsibleSection title="Activity" count={data.recentActivity.length}>
                {data.recentActivity.length > 0 ? (
                  <div className="space-y-4">
                    {data.recentActivity.map((activity) => (
                      <div key={activity.id} className="flex gap-3 items-start">
                        <div className="mt-1 h-2 w-2 rounded-full bg-primary/30" />
                        <div className="flex flex-col">
                          <span className="text-xs text-foreground/80 font-medium">{activity.content}</span>
                          <span className="text-[10px] text-muted-foreground">{format(new Date(activity.timestamp), "MMM d, h:mm a")}</span>
                        </div>
                      </div>
                    ))}
                  </div>
                ) : (
                  <div className="flex flex-col items-center justify-center py-4 text-center opacity-30">
                    <Activity className="h-4 w-4 mb-2" />
                    <span className="text-[10px] font-bold uppercase tracking-widest">
                      No Recent Activity
                    </span>
                  </div>
                )}
              </CollapsibleSection>
            </div>
          </TabsContent>

          <TabsContent
            value="docs"
            className="m-0 p-6 animate-in fade-in slide-in-from-right-1 duration-200"
          >
            <div className="flex flex-col items-center justify-center py-12 text-center opacity-30">
              <FileText className="h-8 w-8 mb-4 stroke-[1.2]" />
              <span className="text-[11px] font-bold uppercase tracking-widest">
                No Linked Docs
              </span>
            </div>
          </TabsContent>
        </ScrollArea>
      </Tabs>
    </div>
  );
}

function TabIconTrigger({ value, icon: Icon }: { value: string; icon: any }) {
  return (
    <TabsTrigger
      value={value}
      className="h-full px-0 bg-transparent shadow-none border-none data-[state=active]:bg-transparent data-[state=active]:shadow-none data-[state=active]:text-primary text-muted-foreground/50 hover:text-foreground transition-all relative group focus-visible:ring-0 focus-visible:ring-offset-0"
    >
      <Icon className="h-4 w-4" />
      <div className="absolute bottom-0 left-0 right-0 h-[2px] bg-primary rounded-t-full scale-x-0 group-data-[state=active]:scale-x-100 transition-transform origin-center" />
    </TabsTrigger>
  );
}

function PropertyRow({
  icon: Icon,
  label,
  children,
}: {
  icon: any;
  label: string;
  children: React.ReactNode;
}) {
  return (
    <div className="grid grid-cols-[110px_1fr] items-center px-5 py-1.5 hover:bg-foreground/[0.02] transition-colors group">
      <div className="flex items-center gap-2.5 text-muted-foreground/50 group-hover:text-muted-foreground transition-colors">
        <Icon className="h-3.5 w-3.5" />
        <span className="text-[11px] font-bold tracking-tight">{label}</span>
      </div>
      <div className="text-[12px]">{children}</div>
    </div>
  );
}

function CollapsibleSection({
  title,
  count,
  children,
}: {
  title: string;
  count: number | null;
  children: React.ReactNode;
}) {
  return (
    <div className="border-b border-border/40">
      <div className="flex items-center justify-between px-5 py-2.5 cursor-pointer hover:bg-foreground/[0.02] transition-colors group">
        <div className="flex items-center gap-2">
          <ChevronDown className="h-3 w-3 text-muted-foreground/30 group-hover:text-muted-foreground transition-colors" />
          <span className="text-[11px] font-black uppercase tracking-widest text-foreground/70">
            {title}
          </span>
        </div>
        {count !== null && count > 0 && (
          <span className="text-[10px] font-black text-muted-foreground/20">
            {count}
          </span>
        )}
      </div>
      <div className="px-5 pb-4 pt-1">{children}</div>
    </div>
  );
}
