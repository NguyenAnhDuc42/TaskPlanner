import { Skeleton } from "@/components/ui/skeleton";

export function TaskViewSkeleton() {
  return (
    <div className="flex flex-col h-full w-full bg-transparent overflow-hidden">
      <div className="flex-1 overflow-y-auto">
        <div className="w-full p-4 md:p-8 space-y-6">
          {/* Header Title Area */}
          <div className="flex items-center gap-3">
            <Skeleton className="h-9 w-9 rounded-lg" />
            <Skeleton className="h-8 w-1/3 rounded-md" />
          </div>

          {/* Properties Area */}
          <div className="flex flex-col gap-3.5 pb-6 border-b border-border/30">
            <div className="flex flex-wrap items-center gap-2.5">
              <Skeleton className="h-7 w-24 rounded-md" />
              <Skeleton className="h-7 w-24 rounded-md" />
              <Skeleton className="h-7 w-32 rounded-md" />
            </div>
            <div className="flex items-center gap-2">
              <div className="flex -space-x-1.5">
                <Skeleton className="h-6 w-6 rounded-full" />
                <Skeleton className="h-6 w-6 rounded-full" />
              </div>
              <Skeleton className="h-6 w-6 rounded-full" />
            </div>
          </div>

          {/* Document Section */}
          <div className="space-y-3 pt-4">
            <Skeleton className="h-4 w-24 rounded-md" />
            <Skeleton className="h-[200px] w-full rounded-lg" />
          </div>
          
          {/* Subtasks Section */}
          <div className="space-y-4 pt-6">
            <Skeleton className="h-4 w-24 rounded-md" />
            <Skeleton className="h-10 w-full rounded-md" />
            <Skeleton className="h-10 w-full rounded-md" />
          </div>
        </div>
      </div>
    </div>
  );
}
