

export function MockItem({
  icon: Icon,
  label,
  count,
}: {
  icon: any;
  label: string;
  count?: number;
}) {
  return (
    <div className="flex items-center gap-2 px-1 py-0.5 rounded-sm hover:bg-muted cursor-pointer group transition-colors">
      <Icon className="h-3.5 w-3.5 text-muted-foreground group-hover:text-foreground transition-colors flex-shrink-0" />
      <span className="text-[11px] font-semibold text-muted-foreground group-hover:text-foreground transition-colors flex-1 truncate">
        {label}
      </span>
      {count !== undefined && (
        <span className="text-[10px] font-mono text-muted-foreground">
          {count}
        </span>
      )}
    </div>
  );
}
