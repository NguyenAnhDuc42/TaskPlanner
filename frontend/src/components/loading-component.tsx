import { Loader2 } from "lucide-react";

export function LoadingComponent() {
  return (
    <div className="flex-1 flex flex-col items-center justify-center w-full h-full min-h-[400px] gap-3 text-muted-foreground bg-[#0d0d0e]/50 backdrop-blur-sm">
      <Loader2 className="h-6 w-6 animate-spin text-primary" />
      <span className="text-[10px] font-black uppercase tracking-widest text-muted-foreground/60">
        Loading...
      </span>
    </div>
  );
}
