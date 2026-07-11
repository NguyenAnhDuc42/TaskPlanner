import { useState } from "react";
import { ChevronRight } from "lucide-react";
import { cn } from "@/lib/utils";

interface ExpandableSectionProps {
  title: string;
  defaultOpen?: boolean;
  children: React.ReactNode;
}

export function ExpandableSection({ title, defaultOpen = false, children }: Readonly<ExpandableSectionProps>) {
  const [open, setOpen] = useState(defaultOpen);

  return (
    <div className="pt-3 border-t border-border/30">
      <button
        type="button"
        onClick={() => setOpen((o) => !o)}
        className="flex items-center gap-1 w-full text-left cursor-pointer group"
      >
        <ChevronRight
          className={cn(
            "h-3 w-3 text-muted-foreground/50 transition-transform duration-150 shrink-0",
            open && "rotate-90"
          )}
        />
        <span className="font-mono text-[10px] uppercase tracking-widest text-muted-foreground/70 group-hover:text-muted-foreground transition-colors">
          {title}
        </span>
      </button>

      {open && <div className="mt-3">{children}</div>}
    </div>
  );
}
