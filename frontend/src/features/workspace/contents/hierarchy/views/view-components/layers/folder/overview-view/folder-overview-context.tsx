import { ScrollArea } from "@/components/ui/scroll-area";
import {
  FileText,
  Calendar,
  Layers,
  ChevronDown,
  Layout,
  Activity,
  CheckCircle2,
  Clock,
} from "lucide-react";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";

import { format } from "date-fns";
import type { OverviewViewData } from "../../../../views-type";

interface FolderOverviewContextProps {
  data?: OverviewViewData | null;
}

export function FolderOverviewContext({ data }: FolderOverviewContextProps) {
  if (!data) return null;

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

              {/* STATUS - ALWAYS SHOW */}
              <PropertyRow icon={Layers} label="Status">
                {data.status ? (
                  <div className="flex items-center gap-2">
                    <div 
                      className="h-2 w-2 rounded-full" 
                      style={{ backgroundColor: data.status.color }}
                    />
                    <span className="font-semibold text-foreground/80">
                      {data.status.category}. {data.status.name}
                    </span>
                  </div>
                ) : (
                  <div className="flex items-center gap-2 opacity-40">
                    <div className="h-2 w-2 rounded-full bg-muted-foreground" />
                    <span className="font-semibold italic">No status</span>
                  </div>
                )}
              </PropertyRow>

              {data.workflowName && (
                <PropertyRow icon={CheckCircle2} label="Workflow">
                  <span className="font-semibold text-foreground/80">
                    {data.workflowName}
                  </span>
                </PropertyRow>
              )}

              <PropertyRow icon={Calendar} label="Start Date">
                <span className="font-semibold text-foreground/80">
                  {data.startDate ? format(new Date(data.startDate), "MMM d, yyyy") : "None"}
                </span>
              </PropertyRow>

              <PropertyRow icon={Clock} label="Due Date">
                <span className="font-semibold text-foreground/80">
                  {data.dueDate ? format(new Date(data.dueDate), "MMM d, yyyy") : "None"}
                </span>
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
