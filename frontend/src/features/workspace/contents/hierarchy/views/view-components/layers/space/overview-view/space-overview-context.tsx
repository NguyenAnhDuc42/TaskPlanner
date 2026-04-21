import { ScrollArea } from "@/components/ui/scroll-area";
import { MessageSquare, Users, GitMerge, Activity } from "lucide-react";

export function SpaceOverviewContext() {
  return (
    <div className="flex-1 flex flex-col h-full bg-muted/5">
      <div className="h-14 px-6 flex items-center justify-between border-b border-border/50 bg-background/50 backdrop-blur-md">
        <span className="text-[10px] font-black uppercase tracking-[0.3em] text-muted-foreground/40 shrink-0">
          Space Management
        </span>
        <div className="flex items-center gap-2 text-muted-foreground/20">
          <Activity className="h-3.5 w-3.5" />
        </div>
      </div>

      <ScrollArea className="flex-1">
        <div className="p-6 space-y-6">
           {/* Space Status Placeholder */}
           <div className="space-y-3">
              <span className="text-[9px] font-black text-muted-foreground/30 uppercase tracking-[0.3em]">
                Layer Status
              </span>
              <div className="p-4 rounded-xl bg-background border border-border/40 hover:border-border/80 transition-colors flex items-center gap-4 cursor-pointer">
                 <div className="h-3 w-3 rounded-full bg-emerald-500 shadow-[0_0_10px_rgba(16,185,129,0.4)]" />
                 <div className="flex flex-col">
                    <span className="text-[13px] font-bold text-foreground/80">Active Phase</span>
                    <span className="text-[10px] font-medium text-muted-foreground/50">Space is currently active</span>
                 </div>
              </div>
           </div>

           {/* Workflow Engine Placeholder */}
           <div className="space-y-3">
              <span className="text-[9px] font-black text-muted-foreground/30 uppercase tracking-[0.3em]">
                Workflow Engine
              </span>
              <div className="p-4 rounded-xl bg-background border border-dashed border-border/40 hover:border-primary/30 hover:bg-primary/5 transition-colors flex items-center gap-4 cursor-pointer group">
                 <div className="p-2 rounded-lg bg-muted group-hover:bg-primary/20 group-hover:text-primary transition-colors">
                    <GitMerge className="h-4 w-4 text-muted-foreground" />
                 </div>
                 <div className="flex flex-col">
                    <span className="text-[12px] font-bold text-foreground/60 group-hover:text-foreground/80">Assign Workflow</span>
                    <span className="text-[10px] font-medium text-muted-foreground/40">Apply status rules to children</span>
                 </div>
              </div>
           </div>

           {/* Chat Room Placeholder */}
           <div className="space-y-3">
              <span className="text-[9px] font-black text-muted-foreground/30 uppercase tracking-[0.3em]">
                Quick Chat Room
              </span>
              <div className="p-4 rounded-xl bg-background border border-dashed border-border/40 hover:border-primary/30 hover:bg-primary/5 transition-colors flex items-center gap-4 cursor-pointer group">
                 <div className="p-2 rounded-lg bg-muted group-hover:bg-primary/20 group-hover:text-primary transition-colors">
                    <MessageSquare className="h-4 w-4 text-muted-foreground" />
                 </div>
                 <div className="flex flex-col">
                    <span className="text-[12px] font-bold text-foreground/60 group-hover:text-foreground/80">Link Chat Room</span>
                    <span className="text-[10px] font-medium text-muted-foreground/40">Dedicated channel for this space</span>
                 </div>
              </div>
           </div>

           {/* Members Placeholder */}
           <div className="space-y-3 pt-4 border-t border-border/20">
              <div className="flex items-center justify-between mb-2">
                 <span className="text-[9px] font-black text-muted-foreground/30 uppercase tracking-[0.3em]">
                   Space Members
                 </span>
                 <Users className="h-3 w-3 text-muted-foreground/30" />
              </div>
              <div className="flex -space-x-2">
                 {[1, 2, 3].map((i) => (
                    <div key={i} className="h-8 w-8 rounded-full bg-muted border-2 border-background flex items-center justify-center relative z-10 hover:z-20 hover:scale-110 transition-transform">
                       <span className="text-[10px] font-bold text-foreground/40">U{i}</span>
                    </div>
                 ))}
                 <div className="h-8 w-8 rounded-full bg-background border-2 border-dashed border-border flex items-center justify-center relative z-0 hover:z-20 hover:border-primary/50 transition-colors cursor-pointer">
                    <span className="text-[14px] font-bold text-muted-foreground/40 leading-none pb-0.5">+</span>
                 </div>
              </div>
           </div>

        </div>
      </ScrollArea>
    </div>
  );
}
