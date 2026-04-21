import { ScrollArea } from "@/components/ui/scroll-area";
import { MessageSquare, Activity } from "lucide-react";

export function FolderOverviewContext() {
  return (
    <div className="flex-1 flex flex-col h-full">
      <div className="h-14 px-6 flex items-center justify-between border-b border-border/50 bg-background/30 backdrop-blur-md">
        <span className="text-[10px] font-black uppercase tracking-[0.3em] text-muted-foreground/40 shrink-0">
          Folder Activity
        </span>
        <div className="flex items-center gap-2 text-muted-foreground/20">
          <MessageSquare className="h-3.5 w-3.5" />
          <Activity className="h-3.5 w-3.5" />
        </div>
      </div>

      <ScrollArea className="flex-1 px-6">
        <div className="py-12 space-y-8">
           {[1, 2].map((i) => (
             <div key={i} className="space-y-3 group cursor-default">
                <div className="flex items-center gap-2">
                   <div className="h-4 w-4 rounded-full bg-emerald-500/10 border border-emerald-500/20" />
                   <span className="text-[10px] font-bold text-muted-foreground/40 uppercase tracking-widest leading-none">
                     Recent Update
                   </span>
                </div>
                <div className="pl-6 border-l-2 border-border/20 group-hover:border-emerald-500/20 transition-colors">
                   <p className="text-[13px] text-foreground/50 leading-relaxed group-hover:text-foreground/70 transition-colors">
                     Folder context was synchronized. Items within this scope are now following the layer-centric visibility rules.
                   </p>
                   <span className="text-[9px] font-medium text-muted-foreground/20 block mt-2">
                     Just now
                   </span>
                </div>
             </div>
           ))}
        </div>
      </ScrollArea>
    </div>
  );
}
