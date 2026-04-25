import { ScrollArea } from "@/components/ui/scroll-area";
import {
  MessageSquare,
  Users,
  GitMerge,
  Activity,
  FileText,
  Info,
} from "lucide-react";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";

export function SpaceOverviewContext() {
  return (
    <div className="flex-1 flex flex-col h-full bg-muted/5">
      <Tabs defaultValue="general" className="flex-1 flex flex-col h-full">
        <div className="h-14 px-6 flex items-center justify-between border-b border-border/50 bg-background/50 backdrop-blur-md">
          <TabsList className="bg-transparent border-none p-0 h-auto gap-4">
            <TabsTrigger
              value="general"
              className="data-[state=active]:bg-transparent data-[state=active]:shadow-none data-[state=active]:text-foreground text-[11px] font-black uppercase tracking-wider p-0 h-auto rounded-none border-b-2 border-transparent data-[state=active]:border-primary transition-all pb-2 mt-2"
            >
              General
            </TabsTrigger>
            <TabsTrigger
              value="docs"
              className="data-[state=active]:bg-transparent data-[state=active]:shadow-none data-[state=active]:text-foreground text-[11px] font-black uppercase tracking-wider p-0 h-auto rounded-none border-b-2 border-transparent data-[state=active]:border-primary transition-all pb-2 mt-2"
            >
              Docs
            </TabsTrigger>
            <TabsTrigger
              value="members"
              className="data-[state=active]:bg-transparent data-[state=active]:shadow-none data-[state=active]:text-foreground text-[11px] font-black uppercase tracking-wider p-0 h-auto rounded-none border-b-2 border-transparent data-[state=active]:border-primary transition-all pb-2 mt-2"
            >
              Members
            </TabsTrigger>
          </TabsList>
          <div className="flex items-center gap-2 text-muted-foreground/20">
            <Activity className="h-3.5 w-3.5" />
          </div>
        </div>

        <ScrollArea className="flex-1">
          <TabsContent
            value="general"
            className="m-0 p-6 space-y-8 animate-in fade-in slide-in-from-right-1 duration-200"
          >
            {/* Space Status */}
            <div className="space-y-3">
              <span className="text-[9px] font-black text-muted-foreground/30 uppercase tracking-[0.3em]">
                Layer Status
              </span>
              <div className="p-4 rounded-xl bg-background border border-border/40 hover:border-border/80 transition-colors flex items-center gap-4 cursor-pointer">
                <div className="h-3 w-3 rounded-full bg-emerald-500 shadow-[0_0_10px_rgba(16,185,129,0.4)]" />
                <div className="flex flex-col">
                  <span className="text-[13px] font-bold text-foreground/80">
                    Active Phase
                  </span>
                  <span className="text-[10px] font-medium text-muted-foreground/50">
                    Space is currently active
                  </span>
                </div>
              </div>
            </div>

            {/* Workflow Engine */}
            <div className="space-y-3">
              <span className="text-[9px] font-black text-muted-foreground/30 uppercase tracking-[0.3em]">
                Workflow Engine
              </span>
              <div className="p-4 rounded-xl bg-background border border-dashed border-border/40 hover:border-primary/30 hover:bg-primary/5 transition-colors flex items-center gap-4 cursor-pointer group">
                <div className="p-2 rounded-lg bg-muted group-hover:bg-primary/20 group-hover:text-primary transition-colors">
                  <GitMerge className="h-4 w-4 text-muted-foreground" />
                </div>
                <div className="flex flex-col">
                  <span className="text-[12px] font-bold text-foreground/60 group-hover:text-foreground/80">
                    Assign Workflow
                  </span>
                  <span className="text-[10px] font-medium text-muted-foreground/40">
                    Apply status rules to children
                  </span>
                </div>
              </div>
            </div>

            {/* Chat Room */}
            <div className="space-y-3">
              <span className="text-[9px] font-black text-muted-foreground/30 uppercase tracking-[0.3em]">
                Quick Chat Room
              </span>
              <div className="p-4 rounded-xl bg-background border border-dashed border-border/40 hover:border-primary/30 hover:bg-primary/5 transition-colors flex items-center gap-4 cursor-pointer group">
                <div className="p-2 rounded-lg bg-muted group-hover:bg-primary/20 group-hover:text-primary transition-colors">
                  <MessageSquare className="h-4 w-4 text-muted-foreground" />
                </div>
                <div className="flex flex-col">
                  <span className="text-[12px] font-bold text-foreground/60 group-hover:text-foreground/80">
                    Link Chat Room
                  </span>
                  <span className="text-[10px] font-medium text-muted-foreground/40">
                    Dedicated channel for this space
                  </span>
                </div>
              </div>
            </div>

            {/* Metadata */}
            <div className="pt-4 border-t border-border/10 space-y-4">
              <div className="flex items-center justify-between">
                <span className="text-[9px] font-black text-muted-foreground/20 uppercase tracking-[0.3em]">
                  Space Identity
                </span>
                <Info className="h-3 w-3 text-muted-foreground/20" />
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div className="flex flex-col gap-0.5">
                  <span className="text-[8px] font-black text-muted-foreground/40 uppercase tracking-widest">
                    Creator
                  </span>
                  <span className="text-[11px] font-bold text-foreground/70">
                    System Admin
                  </span>
                </div>
                <div className="flex flex-col gap-0.5">
                  <span className="text-[8px] font-black text-muted-foreground/40 uppercase tracking-widest">
                    Created
                  </span>
                  <span className="text-[11px] font-bold text-foreground/70">
                    Oct 24, 2024
                  </span>
                </div>
              </div>
            </div>
          </TabsContent>

          <TabsContent
            value="docs"
            className="m-0 p-6 space-y-6 animate-in fade-in slide-in-from-right-1 duration-200"
          >
            <div className="flex flex-col items-center justify-center py-12 px-6 border-2 border-dashed border-border/20 rounded-2xl bg-muted/5 text-center">
              <div className="p-4 rounded-full bg-muted/20 text-muted-foreground/20 mb-4">
                <FileText className="h-8 w-8 stroke-[1.5]" />
              </div>
              <span className="text-[12px] font-bold text-muted-foreground/40 mb-1">
                No Documents Yet
              </span>
              <span className="text-[10px] font-medium text-muted-foreground/20 max-w-[200px]">
                Link external files or internal wiki pages to this space.
              </span>
              <button className="mt-6 px-4 py-2 rounded-lg bg-primary/10 text-primary text-[10px] font-black uppercase tracking-widest hover:bg-primary hover:text-white transition-all">
                Add Document
              </button>
            </div>
          </TabsContent>

          <TabsContent
            value="members"
            className="m-0 p-6 space-y-6 animate-in fade-in slide-in-from-right-1 duration-200"
          >
            <div className="space-y-4">
              <div className="flex items-center justify-between mb-2">
                <span className="text-[9px] font-black text-muted-foreground/30 uppercase tracking-[0.3em]">
                  Active Contributors
                </span>
                <button className="text-[9px] font-black text-primary uppercase tracking-widest hover:underline">
                  Invite
                </button>
              </div>

              <div className="space-y-2">
                {[1, 2, 3].map((i) => (
                  <div
                    key={i}
                    className="flex items-center justify-between p-3 rounded-xl bg-background border border-border/30"
                  >
                    <div className="flex items-center gap-3">
                      <div className="h-8 w-8 rounded-full bg-primary/10 flex items-center justify-center border border-primary/20">
                        <span className="text-[10px] font-bold text-primary">
                          U{i}
                        </span>
                      </div>
                      <div className="flex flex-col">
                        <span className="text-[12px] font-bold text-foreground/80">
                          User Number {i}
                        </span>
                        <span className="text-[9px] font-medium text-muted-foreground/40">
                          Collaborator
                        </span>
                      </div>
                    </div>
                    <div className="h-2 w-2 rounded-full bg-emerald-500/40" />
                  </div>
                ))}
              </div>

              <div className="p-4 rounded-2xl bg-primary/5 border border-primary/10 flex flex-col gap-2 mt-8">
                <span className="text-[10px] font-black text-primary uppercase tracking-widest">
                  Team Insight
                </span>
                <span className="text-[11px] font-medium text-primary/70 leading-relaxed">
                  This space is currently managed by 3 core members. Add more
                  people to increase collaboration.
                </span>
              </div>
            </div>
          </TabsContent>
        </ScrollArea>
      </Tabs>
    </div>
  );
}
