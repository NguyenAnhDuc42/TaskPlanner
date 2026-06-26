import { Loader2 } from "lucide-react";
import { cn } from "@/lib/utils";

interface ViewSkeletonProps {
  className?: string;
}

export function ViewSkeleton({ className }: ViewSkeletonProps) {
  return (
    <div className={cn("flex-1 flex flex-col items-center justify-center w-full h-full min-h-[400px] gap-4 bg-transparent", className)}>
      <div className="relative flex items-center justify-center">
        <div className="absolute inset-0 bg-primary/20 blur-xl rounded-full scale-150" />
        <Loader2 className="h-6 w-6 animate-spin text-primary relative z-10" />
      </div>
      <div className="flex flex-col items-center gap-1 text-center mt-2">
        <h3 className="text-sm font-semibold text-foreground/80 tracking-tight">Syncing View</h3>
        <p className="text-[10px] text-muted-foreground/50 uppercase tracking-widest font-bold">Please wait</p>
      </div>
    </div>
  );
}
