import { Loader2 } from "lucide-react";
import { cn } from "@/lib/utils";

interface LoadingScreenProps {
  label?: string;
  fullScreen?: boolean;
  className?: string;
}

export function LoadingScreen({ label, fullScreen = false, className }: Readonly<LoadingScreenProps>) {
  return (
    <div
      className={cn(
        "flex flex-col items-center justify-center gap-3",
        fullScreen ? "h-screen w-full bg-background" : "flex-1 w-full h-full min-h-[400px]",
        className,
      )}
    >
      <div className="relative flex items-center justify-center">
        <div className="absolute inset-0 bg-primary/20 blur-xl rounded-full scale-150" />
        <Loader2 className="h-6 w-6 animate-spin text-primary relative z-10" />
      </div>
      {label && (
        <span className="text-[10px] font-black uppercase tracking-widest text-muted-foreground/60">
          {label}
        </span>
      )}
    </div>
  );
}
