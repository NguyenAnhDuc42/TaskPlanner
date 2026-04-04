import { useSidebarContext } from "@/features/workspace/components/sidebar-provider";
import { useHierarchy } from "./hierarchy-api";
import {
  Loader2,
  ChevronRight,
  Plus,
  Lock,
  CheckSquare,
  Search,
  FileText,
  Clock,
  ChevronDown,
} from "lucide-react";
import * as Icons from "lucide-react";
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from "@/components/ui/collapsible";
import { cn } from "@/lib/utils";
import { ScrollArea } from "@/components/ui/scroll-area";
import { useNavigate, useLocation } from "@tanstack/react-router";
import { useState, useMemo } from "react";
import type {
  SpaceHierarchy,
  FolderHierarchy,
  TaskHierarchy,
} from "./hierarchy-type";

const NAME_CHAR_LIMIT = 20;

function clampName(name: string, limit = NAME_CHAR_LIMIT) {
  if (name.length <= limit) return name;
  return `${name.slice(0, Math.max(0, limit - 1))}…`;
}

export function HierarchySidebar() {
  const { workspaceId } = useSidebarContext();
  const { data: hierarchy, isLoading, error } = useHierarchy(workspaceId || "");
  const [isHierarchyOpen, setIsHierarchyOpen] = useState(true);
  const [isDocsOpen, setIsDocsOpen] = useState(true);
  const [searchQuery, setSearchQuery] = useState("");

  const filteredHierarchy = useMemo(() => {
    if (!hierarchy || !searchQuery) return hierarchy;
    const query = searchQuery.toLowerCase();
    const filteredSpaces = hierarchy.spaces.map(space => {
      const folders = space.folders.filter(f => f.name.toLowerCase().includes(query));
      const tasksInSpace = space.tasks.filter(t => t.name.toLowerCase().includes(query));
      const isSpaceMatch = space.name.toLowerCase().includes(query);
      if (isSpaceMatch || folders.length > 0 || tasksInSpace.length > 0) {
        return { ...space, folders, tasks: tasksInSpace, isExpanded: true };
      }
      return null;
    }).filter(s => s !== null) as SpaceHierarchy[];
    return { ...hierarchy, spaces: filteredSpaces };
  }, [hierarchy, searchQuery]);

  if (isLoading) {
    return (
      <div className="flex flex-col items-center justify-center h-40 gap-3 text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        <span className="text-[10px] font-bold uppercase tracking-widest">Loading...</span>
      </div>
    );
  }

  if (error) return null;

  return (
    <div className="h-full flex flex-col bg-background overflow-hidden select-none">
      {/* Search & Actions */}
      <div className="px-1 pb-1 pt-0 border-b border-border flex-shrink-0 flex items-center gap-1">
        <div className="flex items-center gap-2 px-2 h-7 rounded-sm bg-muted border border-border focus-within:border-primary/60 transition-colors group flex-1">
          <Search className="h-3 w-3 text-muted-foreground flex-shrink-0" />
          <input
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder="Search..."
            className="flex-1 bg-transparent border-none outline-none text-[10px] font-semibold text-foreground placeholder:text-muted-foreground"
          />
          {searchQuery && (
            <button
              onClick={() => setSearchQuery("")}
              className="text-xs text-muted-foreground hover:text-foreground transition-colors"
            >
              ✕
            </button>
          )}
        </div>
        <button className="h-7 w-7 flex-shrink-0 flex items-center justify-center rounded-sm text-muted-foreground hover:text-foreground hover:bg-muted transition-colors" title="Create Space">
          <Plus className="h-4 w-4" />
        </button>
      </div>

      <ScrollArea className="flex-1 min-h-0">
        <div className="py-2">

          {/* NAVIGATION SECTION */}
          <Collapsible open={isHierarchyOpen} onOpenChange={setIsHierarchyOpen}>
            <CollapsibleTrigger className="w-full flex items-center gap-2 px-1 py-0.5 hover:bg-muted transition-colors group rounded-sm">
              <ChevronDown className={cn("h-3 w-3 text-muted-foreground transition-transform duration-200", !isHierarchyOpen && "-rotate-90")} />
              <span className="text-[10px] font-bold text-muted-foreground uppercase tracking-wider flex-1 text-left">Navigation</span>
              <Plus className="h-3 w-3 text-muted-foreground opacity-0 group-hover:opacity-100 transition-opacity hover:text-primary" />
            </CollapsibleTrigger>
            <CollapsibleContent>
              <div className="px-2 pt-0.5 pb-2 flex flex-col gap-0.5">
                {filteredHierarchy?.spaces.map((space) => (
                  <SpaceItem key={space.id} space={space} isForcedOpen={!!searchQuery} />
                ))}
              </div>
            </CollapsibleContent>
          </Collapsible>

          {/* DOCS & TASKS SECTION */}
          <Collapsible open={isDocsOpen} onOpenChange={setIsDocsOpen}>
            <CollapsibleTrigger className="w-full flex items-center gap-2 px-1 py-0.5 hover:bg-muted transition-colors group mt-1 pt-1 border-t border-border rounded-sm">
              <ChevronDown className={cn("h-3 w-3 text-muted-foreground transition-transform duration-200", !isDocsOpen && "-rotate-90")} />
              <span className="text-[10px] font-bold text-muted-foreground uppercase tracking-wider text-left">Docs & Tasks</span>
            </CollapsibleTrigger>
            <CollapsibleContent>
              <div className="px-2 pt-0.5 pb-2 flex flex-col gap-0.5">
                <MockItem icon={FileText} label="Product Roadmap" count={3} />
                <MockItem icon={FileText} label="Design Systems" count={12} />
                <MockItem icon={Clock} label="Recent Tasks" />
              </div>
            </CollapsibleContent>
          </Collapsible>

        </div>
      </ScrollArea>
    </div>
  );
}

function MockItem({ icon: Icon, label, count }: { icon: any; label: string; count?: number }) {
  return (
    <div className="flex items-center gap-2 px-1 py-0.5 rounded-sm hover:bg-muted cursor-pointer group transition-colors">
      <Icon className="h-3.5 w-3.5 text-muted-foreground group-hover:text-foreground transition-colors flex-shrink-0" />
      <span className="text-[11px] font-semibold text-muted-foreground group-hover:text-foreground transition-colors flex-1 truncate">{label}</span>
      {count !== undefined && (
        <span className="text-[10px] font-mono text-muted-foreground">{count}</span>
      )}
    </div>
  );
}

function SpaceItem({ space, isForcedOpen }: { space: SpaceHierarchy; isForcedOpen?: boolean }) {
  const [isOpen, setIsOpen] = useState(true);
  const { workspaceId } = useSidebarContext();
  const navigate = useNavigate();
  const location = useLocation();
  const isActive = location.pathname.includes(`/spaces/${space.id}`);
  const IconComponent = (Icons as any)[space.icon] || Icons.LayoutGrid;
  const spaceColor = space.color || "var(--primary)";
  const effectiveOpen = isForcedOpen || isOpen;

  return (
    <Collapsible open={effectiveOpen} onOpenChange={setIsOpen} className="w-full">
      <div
        className={cn(
          "flex items-center w-full px-1 py-0.5 rounded-sm transition-colors cursor-pointer mb-px",
          isActive ? "bg-primary/10 text-primary" : "text-foreground hover:bg-muted"
        )}
        onClick={() => navigate({ to: `/workspaces/${workspaceId}/spaces/${space.id}` })}
      >
        <div
          className="relative flex items-center justify-center w-5 h-5 flex-shrink-0 cursor-pointer rounded-sm hover:bg-background/50 group/icon mr-0.5"
          onClick={(e) => { e.stopPropagation(); setIsOpen(!isOpen); }}
        >
          <IconComponent className="h-3.5 w-3.5 absolute transition-opacity group-hover/icon:opacity-0" style={{ color: isActive ? spaceColor : undefined }} />
          <ChevronRight className={cn("h-4 w-4 absolute opacity-0 transition-all text-muted-foreground group-hover/icon:opacity-100", isOpen && "rotate-90")} />
        </div>
        <span className="truncate text-[11px] font-bold flex-1">{clampName(space.name)}</span>
        {space.isPrivate && <Lock className="h-3 w-3 text-muted-foreground flex-shrink-0 opacity-40 ml-1" />}
      </div>

      <CollapsibleContent className="ml-3 pl-1 border-l border-border mt-0.5 flex flex-col">
        {space.folders.map((folder) => (
          <FolderItem key={folder.id} folder={folder} />
        ))}
        {space.tasks.map((task) => (
          <TaskItem key={task.id} task={task} />
        ))}
      </CollapsibleContent>
    </Collapsible>
  );
}

function FolderItem({ folder }: { folder: FolderHierarchy }) {
  const [isOpen, setIsOpen] = useState(false);
  const IconComponent = (Icons as any)[folder.icon] || Icons.Folder;
  const location = useLocation();
  const isActive = location.pathname.includes(`/folders/${folder.id}`);
  const navigate = useNavigate();
  const { workspaceId } = useSidebarContext();

  return (
    <Collapsible open={isOpen} onOpenChange={setIsOpen} className="w-full">
      <div
        className={cn(
          "flex items-center w-full px-1 py-0.5 rounded-sm transition-colors cursor-pointer mb-px",
          isActive ? "bg-primary/10 text-foreground" : "text-muted-foreground hover:bg-muted hover:text-foreground"
        )}
        onClick={() => navigate({ to: `/workspaces/${workspaceId}/folders/${folder.id}` })}
      >
        <div
          className="relative flex items-center justify-center w-5 h-5 flex-shrink-0 cursor-pointer rounded-sm hover:bg-background/50 group/icon mr-0.5"
          onClick={(e) => { e.stopPropagation(); setIsOpen(!isOpen); }}
        >
          <IconComponent className="h-3.5 w-3.5 absolute transition-opacity group-hover/icon:opacity-0" style={{ color: folder.color }} />
          <ChevronRight className={cn("h-4 w-4 absolute opacity-0 transition-all text-muted-foreground group-hover/icon:opacity-100", isOpen && "rotate-90")} />
        </div>
        <span className="truncate text-[11px] font-semibold flex-1">{clampName(folder.name)}</span>
      </div>
      <CollapsibleContent className="ml-3 pl-1 border-l border-border mt-0.5 flex flex-col">
        {folder.tasks.map((task) => (
          <TaskItem key={task.id} task={task} />
        ))}
      </CollapsibleContent>
    </Collapsible>
  );
}

function TaskItem({ task }: { task: TaskHierarchy }) {
  const navigate = useNavigate();
  const { workspaceId } = useSidebarContext();
  const location = useLocation();
  const isActive = location.pathname.includes(`/tasks/${task.id}`);

  return (
    <div
      className={cn(
        "flex items-center w-full px-1 py-0.5 rounded-sm transition-colors cursor-pointer mb-px pl-6",
        isActive ? "text-primary bg-primary/10" : "text-muted-foreground hover:bg-muted hover:text-foreground"
      )}
      onClick={() => navigate({ to: `/workspaces/${workspaceId}/tasks/${task.id}` })}
    >
      <CheckSquare className="h-3.5 w-3.5 flex-shrink-0 opacity-60 mr-1.5" />
      <span className="truncate text-[11px] font-semibold flex-1 leading-tight">{clampName(task.name)}</span>
    </div>
  );
}
