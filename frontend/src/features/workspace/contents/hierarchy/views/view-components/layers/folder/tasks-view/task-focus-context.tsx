import { ScrollArea } from "@/components/ui/scroll-area";
import { MessageSquare, User, Flag } from "lucide-react";

interface TaskFocusContextProps {
  taskId: string;
  taskName: string;
}

export function TaskFocusContext({
  taskName,
}: TaskFocusContextProps) {
  return (
    <div className="flex-1 flex flex-col h-full bg-muted/5">
      {/* Header */}
      <div className="h-14 px-6 flex flex-col justify-center border-b border-border/50 bg-background/50 backdrop-blur-md">
        <span className="text-[9px] font-black uppercase tracking-[0.2em] text-primary/40 leading-none mb-1">
          Task Intelligence
        </span>
        <span className="text-[13px] font-black text-foreground/80 tracking-tight italic">
          {taskName}
        </span>
      </div>

      <ScrollArea className="flex-1">
        <div className="p-6 space-y-8">
           {/* Properties Placeholder */}
           <div className="space-y-4">
              <span className="text-[9px] font-black text-muted-foreground/30 uppercase tracking-[0.3em]">
                Quick Properties
              </span>
              <div className="grid grid-cols-2 gap-3">
                 <div className="p-3 rounded-xl bg-background border border-border/40 flex items-center gap-3">
                    <User className="h-3.5 w-3.5 text-muted-foreground/40" />
                    <span className="text-[11px] font-bold text-foreground/60">Duc</span>
                 </div>
                 <div className="p-3 rounded-xl bg-background border border-border/40 flex items-center gap-3">
                    <Flag className="h-3.5 w-3.5 text-orange-500/50" />
                    <span className="text-[11px] font-bold text-foreground/60 uppercase tracking-widest">High</span>
                 </div>
              </div>
           </div>

           {/* Description Placeholder */}
           <div className="space-y-4">
              <span className="text-[9px] font-black text-muted-foreground/30 uppercase tracking-[0.3em] block border-b border-border/20 pb-2">
                Context & Requirements
              </span>
              <p className="text-[13px] text-foreground/50 leading-relaxed italic">
                Awaiting detailed technical requirements for this task specific to the folder scope.
              </p>
           </div>

           {/* Activity/Chat Placeholder */}
           <div className="space-y-6 pt-6 border-t border-border/20">
              <div className="flex items-center justify-between">
                 <span className="text-[9px] font-black text-muted-foreground/30 uppercase tracking-[0.2em]">
                   Discussion
                 </span>
                 <MessageSquare className="h-3.5 w-3.5 text-muted-foreground/20" />
              </div>
              
              <div className="h-px w-full bg-border/20" />
              
              <div className="py-20 flex flex-col items-center justify-center gap-2 opacity-50">
                 <div className="h-10 w-10 rounded-full border-2 border-dashed border-primary/20 animate-pulse" />
                 <span className="text-[10px] font-black uppercase tracking-widest text-muted-foreground/20">
                   Stream Offline
                 </span>
              </div>
           </div>
        </div>
      </ScrollArea>
    </div>
  );
}
