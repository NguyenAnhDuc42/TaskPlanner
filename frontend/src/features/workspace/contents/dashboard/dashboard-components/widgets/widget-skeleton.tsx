import { Skeleton } from "@/components/ui/skeleton";

/**
 * A skeleton loader that mimics the shape of a typical widget's content.
 * Renders pulsing placeholders for stat cards and list items.
 */
export function WidgetSkeleton() {
  return (
    <div className="flex flex-col h-full animate-in fade-in duration-300">
      {/* Stat Cards Row Skeleton */}
      <div className="grid grid-cols-3 gap-2 p-3 border-b bg-muted/5">
        {Array.from({ length: 3 }).map((_, i) => (
          <div key={i} className="flex flex-col items-center justify-center p-2 rounded-lg border bg-background gap-1.5">
            <Skeleton className="h-3.5 w-3.5 rounded-full" />
            <Skeleton className="h-5 w-8" />
            <Skeleton className="h-2 w-10" />
          </div>
        ))}
      </div>

      {/* List Items Skeleton */}
      <div className="flex-1 p-2 space-y-2">
        {Array.from({ length: 4 }).map((_, i) => (
          <div key={i} className="flex flex-col gap-1.5 p-2.5 rounded-lg border bg-background">
            <div className="flex items-center justify-between gap-2">
              <Skeleton className="h-3 w-[60%]" />
              <Skeleton className="h-4 w-12 rounded-sm" />
            </div>
            <div className="flex items-center gap-3">
              <Skeleton className="h-2.5 w-20" />
              <Skeleton className="h-1 w-1 rounded-full" />
              <Skeleton className="h-2.5 w-16" />
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
