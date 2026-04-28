import { cn } from "@/lib/utils";
import { Plus, ChevronRight } from "lucide-react";
import { Button } from "@/components/ui/button";
import type { ViewDto } from "../../views-type";

interface ViewHeaderProps {
  entityName: string;
  entityType: string;
  parentName?: string;
  views?: ViewDto[];
  activeViewId: string | null;
  onViewChange: (id: string) => void;
  isContextOpen: boolean;
  onContextToggle: () => void;
}

export function ViewHeader({
  entityName,
  entityType,
  parentName,
  views,
  activeViewId,
  onViewChange,
  isContextOpen,
  onContextToggle,
}: ViewHeaderProps) {
  return (
    <div className="h-12 flex flex-col flex-shrink-0 z-30 px-6 border-b border-border/40">

      {/* VIEW TABS & ACTIONS */}
      <div className="flex-1 flex items-center justify-between h-full">
        <div className="flex items-center gap-3 h-full">
          {views?.map((v) => (
            <button
              key={v.id}
              onClick={() => onViewChange(v.id)}
              className={cn(
                "h-full px-1 text-[11px] font-black uppercase tracking-wider transition-all relative",
                activeViewId === v.id
                  ? "text-foreground"
                  : "text-muted-foreground hover:text-foreground",
              )}
            >
              {v.name}
              {activeViewId === v.id && (
                <div className="absolute bottom-0 left-0 right-0 h-[2px] bg-primary rounded-t-full" />
              )}
            </button>
          ))}
          <button className="h-full px-1 text-muted-foreground hover:text-foreground transition-colors">
            <Plus className="h-3.5 w-3.5" />
          </button>
        </div>

        {/* RIGHT ACTIONS */}
        <div className="flex items-center h-full">
          <Button
            variant="ghost"
            size="icon"
            className="h-5 w-5 text-muted-foreground hover:text-foreground"
            onClick={onContextToggle}
          >
            <svg
              width="15"
              height="15"
              viewBox="0 0 15 15"
              fill="none"
              xmlns="http://www.w3.org/2000/svg"
              className="h-3.5 w-3.5"
            >
              <path
                d="M1.5 3C1.22386 3 1 3.22386 1 3.5C1 3.77614 1.22386 4 1.5 4H13.5C13.7761 4 14 3.77614 14 3.5C14 3.22386 13.7761 3 13.5 3H1.5ZM1 7.5C1 7.22386 1.22386 7 1.5 7H13.5C13.7761 7 14 7.22386 14 7.5C14 7.77614 13.7761 8 13.5 8H1.5C1.22386 8 1 7.77614 1 7.5ZM1 11.5C1 11.2239 1.22386 11 1.5 11H13.5C13.7761 11 14 11.2239 14 11.5C14 11.7761 13.7761 12 13.5 12H1.5C1.22386 12 1 11.7761 1 11.5Z"
                fill="currentColor"
                fillRule="evenodd"
                clipRule="evenodd"
              ></path>
            </svg>
          </Button>
        </div>
      </div>
    </div>
  );
}
