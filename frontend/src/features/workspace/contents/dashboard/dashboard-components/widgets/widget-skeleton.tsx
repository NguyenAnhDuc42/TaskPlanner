import { Skeleton } from "@/components/ui/skeleton";

/**
 * A skeleton loader that mimics the shape of a typical widget's content.
 * Renders pulsing placeholders for stat cards and list items.
 */
export function WidgetSkeleton() {
  return (
    <div className="flex flex-col h-full animate-in fade-in duration-300 bg-transparent">
      {/* Stat Cards Row Skeleton */}
      <div className="grid grid-cols-3 gap-2 p-3 border-b border-border/10">
        {Array.from({ length: 3 }).map((_, i) => (
          <div key={i} className="flex flex-col items-center justify-center p-2 rounded-lg border border-border/10 bg-[var(--theme-item-normal)] gap-1.5 grayscale opacity-40">
            <Skeleton className="h-3.5 w-3.5 rounded-full bg-[var(--theme-text-normal)]" />
            <Skeleton className="h-5 w-8 bg-[var(--theme-text-normal)]" />
            <Skeleton className="h-2 w-10 bg-[var(--theme-text-normal)]" />
          </div>
        ))}
      </div>

      {/* List Items Skeleton */}
      <div className="flex-1 p-2 space-y-1">
        {Array.from({ length: 4 }).map((_, i) => (
          <div key={i} className="flex flex-col gap-1.5 p-2.5 rounded-lg border border-transparent bg-[var(--theme-item-normal)] opacity-30 grayscale">
            <div className="flex items-center justify-between gap-2">
              <Skeleton className="h-3 w-[60%] bg-[var(--theme-text-normal)]" />
              <Skeleton className="h-4 w-12 rounded-sm bg-[var(--theme-text-normal)]" />
            </div>
            <div className="flex items-center gap-3">
              <Skeleton className="h-2.5 w-20 bg-[var(--theme-text-normal)]" />
              <Skeleton className="h-1 w-1 rounded-full bg-[var(--theme-text-normal)]" />
              <Skeleton className="h-2.5 w-16 bg-[var(--theme-text-normal)]" />
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
