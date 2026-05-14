import { cn } from "@/lib/utils";

interface ViewSkeletonProps {
  className?: string;
}

export function ViewSkeleton({ className }: ViewSkeletonProps) {
  return (
    <div className={cn("flex-1 flex flex-col h-full bg-background overflow-hidden relative animate-pulse", className)}>
      {/* 1. Header Skeleton (matches FolderHeader/SpaceHeader h-9) */}
      <div className="flex items-center justify-between px-4 h-9 bg-background/80 backdrop-blur-md border-b border-border/40 flex-shrink-0">
        <div className="flex items-center gap-1.5 flex-1">
          <div className="h-3.5 w-16 rounded bg-muted/20" />
          <div className="h-3 w-3 rounded bg-muted/20" />
          <div className="h-3.5 w-24 rounded bg-muted/20" />
        </div>
        <div className="flex items-center gap-2">
          <div className="h-4 w-8 rounded bg-muted/20" />
          <div className="h-5 w-5 rounded bg-muted/20" />
        </div>
      </div>

      {/* 2. Tabs Skeleton (matches LayerTabs h-8) */}
      <div className="flex items-center gap-1 px-4 h-8 bg-background/50 border-b border-border/40 flex-shrink-0">
        <div className="h-5 w-16 rounded bg-muted/20" />
        <div className="h-5 w-16 rounded bg-muted/20" />
      </div>

      {/* 3. Content Area Skeleton */}
      <div className="flex-1 p-6 space-y-6 overflow-hidden">
        {/* Title Skeleton (matches the large input in Overview) */}
        <div className="h-12 w-1/2 rounded-lg bg-muted/20" />

        {/* Properties Row Skeleton (matches the small blocks) */}
        <div className="flex items-center gap-1.5 flex-wrap">
          <div className="h-6 w-20 rounded-md bg-muted/20" />
          <div className="h-6 w-24 rounded-md bg-muted/20" />
          <div className="h-6 w-16 rounded-md bg-muted/20" />
        </div>

        {/* Fake content blocks */}
        <div className="grid grid-cols-3 gap-4 mt-8">
          <div className="h-32 rounded-xl bg-muted/20 border border-border/5" />
          <div className="h-32 rounded-xl bg-muted/20 border border-border/5" />
          <div className="h-32 rounded-xl bg-muted/20 border border-border/5" />
        </div>
        <div className="h-40 rounded-xl bg-muted/20 border border-border/5" />
      </div>
    </div>
  );
}

