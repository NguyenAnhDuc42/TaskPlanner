import { 
  ChevronDown, 
  Search 
} from "lucide-react";

export function HierarchySidebarSkeleton() {
  return (
    <div className="flex flex-col h-full bg-background border-r border-border/40 w-full text-foreground">
      {/* Search */}
      <div className="h-8 px-1 flex items-center border-b border-border flex-shrink-0">
        <div className="flex items-center gap-2 px-2 h-6 rounded-sm bg-muted/40 border border-border/10 flex-1">
          <Search className="h-3 w-3 text-muted-foreground/40 flex-shrink-0" />
          <div className="text-[10px] font-medium text-muted-foreground/30">
            Search...
          </div>
        </div>
      </div>

      {/* Content Area */}
      <div className="flex-1 overflow-y-auto px-2">
        {/* Section: ITEMS */}
        <div className="mb-4">
          <div className="flex items-center gap-1.5 px-1 py-1 text-[10px] font-black uppercase tracking-wider text-muted-foreground cursor-pointer hover:text-foreground">
            <ChevronDown className="h-3 w-3" />
            Items
          </div>

          <div className="mt-1 space-y-1">
            {/* Skeleton Item 1 */}
            <div className="flex items-center justify-between px-2 py-1.5">
              <div className="flex items-center gap-2">
                <div className="h-4 w-4 bg-muted/60 animate-pulse rounded-sm" />
                <div className="h-3 w-16 bg-muted/60 animate-pulse rounded-sm" />
              </div>
            </div>

            {/* Skeleton Item 2 */}
            <div className="flex items-center justify-between px-2 py-1.5">
              <div className="flex items-center gap-2">
                <div className="h-4 w-4 bg-muted/60 animate-pulse rounded-sm" />
                <div className="h-3 w-24 bg-muted/60 animate-pulse rounded-sm" />
              </div>
            </div>

            {/* Skeleton Item 3 */}
            <div className="flex items-center justify-between px-2 py-1.5">
              <div className="flex items-center gap-2">
                <div className="h-4 w-4 bg-muted/60 animate-pulse rounded-sm" />
                <div className="h-3 w-32 bg-muted/60 animate-pulse rounded-sm" />
              </div>
              <div className="h-3 w-3 bg-muted/40 animate-pulse rounded-sm" />
            </div>

            {/* Skeleton Item 4 */}
            <div className="flex items-center justify-between px-2 py-1.5">
              <div className="flex items-center gap-2">
                <div className="h-4 w-4 bg-muted/60 animate-pulse rounded-sm" />
                <div className="h-3 w-14 bg-muted/60 animate-pulse rounded-sm" />
              </div>
            </div>

            {/* Skeleton Item 5 */}
            <div className="flex items-center justify-between px-2 py-1.5">
              <div className="flex items-center gap-2">
                <div className="h-4 w-4 bg-muted/60 animate-pulse rounded-sm" />
                <div className="h-3 w-28 bg-muted/60 animate-pulse rounded-sm" />
              </div>
            </div>

            {/* ADD ITEM Skeleton */}
            <div className="flex items-center gap-2 px-2 py-1.5">
              <div className="h-3.5 w-3.5 bg-muted/40 animate-pulse rounded-sm" />
              <div className="h-3 w-12 bg-muted/40 animate-pulse rounded-sm" />
            </div>
          </div>
        </div>
      </div>

      {/* Footer / Bottom Section */}
      <div className="p-2 border-t border-border/40">
        <div className="flex items-center gap-1.5 px-1 py-1 text-[10px] font-black uppercase tracking-wider text-muted-foreground cursor-pointer hover:text-foreground">
          <ChevronDown className="h-3 w-3 rotate-[-90deg] transition-transform" />
          Docs & Tasks
        </div>
      </div>
    </div>
  );
}
