const SKELETON_WIDTHS = ["w-16", "w-24", "w-32", "w-14", "w-28"] as const;

export function HierarchySidebarSkeleton() {
  return (
    <div className="flex flex-col w-full py-1">
      <div className="space-y-[2px]">
        {SKELETON_WIDTHS.map((w, i) => (
          <div key={i} className="flex items-center justify-between px-2 py-1.5 rounded-md mx-1">
            <div className="flex items-center gap-2">
              <div className="h-3.5 w-3.5 bg-muted/30 animate-pulse rounded-[3px]" />
              <div className={`h-2.5 ${w} bg-muted/30 animate-pulse rounded-full`} />
            </div>
            {i % 2 === 0 && <div className="h-2.5 w-2.5 bg-muted/20 animate-pulse rounded-full" />}
          </div>
        ))}
      </div>
    </div>
  );
}
