import { EntityLayerType } from "@/types/relationship-type";
import { useEntityInfo, useHierarchy } from "../hierarchy-api";
import * as Icons from "lucide-react";
import { cn } from "@/lib/utils";
import { useState, useMemo } from "react";
import {
  Info,
  MessageSquare,
  Paperclip,
  ChevronLeft,
  ChevronRight,
  Maximize2,
  Plus,
  Settings2,
  List as ListIcon,
  LayoutDashboard,
  FileText,
  Search,
  Filter,
  ArrowDownUp,
  Group as GroupIcon,
  PanelRightClose,
  PanelRight,
} from "lucide-react";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Button } from "@/components/ui/button";
import { ViewType } from "@/types/view-type";

// Dispatcher
import { ViewContainer } from "./view-components/view-container";

// Layer-specific Prop Shells (from the new view-components location)
import { SpaceProps } from "./view-components/space/space-props";
import { FolderProps } from "./view-components/folder/folder-props";
import { TaskProps } from "./view-components/task/task-props";

interface ViewsDisplayerProps {
  workspaceId: string;
  entityId: string;
  layerType: EntityLayerType;
}

// --- MOCK DATA ---
const MOCK_TABS = [
  { id: "v1", name: "List", type: ViewType.List, icon: ListIcon },
  { id: "v2", name: "Board", type: ViewType.Board, icon: LayoutDashboard },
  { id: "v3", name: "Docs", type: "doc" as ViewType, icon: FileText },
];

const MOCK_STATUSES = [
  { id: "s1", name: "In Progress", color: "text-blue-400", category: "active" },
  { id: "s2", name: "Todo", color: "text-gray-400", category: "backlog" },
  {
    id: "s3",
    name: "Completed",
    color: "text-emerald-400",
    category: "completed",
  },
];

export function ViewsDisplayer({
  workspaceId,
  entityId,
  layerType,
}: ViewsDisplayerProps) {
  const entityInfo = useEntityInfo(workspaceId, entityId);
  const { data: hierarchy } = useHierarchy(workspaceId || "");
  const [isPropsOpen, setIsPropsOpen] = useState(true);
  const [activeTab, setActiveTab] = useState<
    "info" | "comments" | "attachments"
  >("info");
  const [activeViewId, setActiveViewId] = useState(MOCK_TABS[0].id);

  const activeView = useMemo(
    () => MOCK_TABS.find((t) => t.id === activeViewId) || MOCK_TABS[0],
    [activeViewId],
  );

  // Dynamically compute tasks from mock hierarchy for testing
  const activeData = useMemo(() => {
    if (!hierarchy) return { tasks: [], statuses: MOCK_STATUSES };

    let rawTasks: any[] = [];
    if (layerType === EntityLayerType.ProjectSpace) {
      const space = hierarchy.spaces.find((s) => s.id === entityId);
      if (space) {
        rawTasks = [...space.tasks, ...space.folders.flatMap((f) => f.tasks)];
      }
    } else if (layerType === EntityLayerType.ProjectFolder) {
      for (const s of hierarchy.spaces) {
        const folder = s.folders.find((f) => f.id === entityId);
        if (folder) {
          rawTasks = folder.tasks;
          break;
        }
      }
    }

    const tasks = rawTasks.map((t, i) => {
      const status = MOCK_STATUSES[i % MOCK_STATUSES.length];
      return {
        id: t.id,
        name: t.name,
        statusId: status.id,
        status: status.name,
        priority:
          t.priority === 1 ? "High" : t.priority === 2 ? "Medium" : "Low",
        assignee: i % 2 === 0 ? "Duc" : "Unassigned",
      };
    });

    return { tasks, statuses: MOCK_STATUSES };
  }, [hierarchy, entityId, layerType]);

  // Mock View DTO
  const activeViewDto = useMemo(
    () => ({
      id: activeView.id,
      workspaceId,
      name: activeView.name,
      viewType: activeView.type,
      isDefault: false,
      displayConfigJson: JSON.stringify({
        groupBy: "status",
        visibleColumns: ["assignee", "dueDate", "priority"],
      }),
    }),
    [activeView, workspaceId],
  );

  if (!entityInfo) return null;

  const iconName = entityInfo.icon || "HelpCircle";
  const IconComponent = (Icons as any)[iconName] || Icons.HelpCircle;

  return (
    <div className="flex-1 flex overflow-hidden h-full bg-transparent gap-1">
      {/* MAIN CONTENT AREA */}
      <div className="flex-1 flex flex-col min-w-0 h-full bg-background border border-border overflow-hidden rounded-xl shadow-sm">
        {/* HEADER: Identity of the Space/Folder/Task */}
        <div className="flex items-center gap-3 px-6 h-12 flex-shrink-0 bg-background border-b border-border z-30">
          <div
            className="p-1.5 rounded-lg bg-muted"
            style={{ color: entityInfo.color }}
          >
            <IconComponent className="h-4 w-4" />
          </div>
          <div className="flex flex-col">
            <span className="text-[14px] font-black tracking-tight text-foreground/90">
              {entityInfo.name}
            </span>
            <span className="text-[9px] font-bold uppercase tracking-widest text-muted-foreground/40 leading-none">
              {entityInfo.type} Context
            </span>
          </div>

          {/* TAB BAR */}
          <div className="ml-8 flex items-center gap-1 h-full pt-1">
            {MOCK_TABS.map((tab) => (
              <button
                key={tab.id}
                onClick={() => setActiveViewId(tab.id)}
                className={cn(
                  "px-4 h-full flex items-center gap-2 text-[11px] font-bold transition-all relative group",
                  activeViewId === tab.id
                    ? "text-primary border-b-2 border-primary"
                    : "text-muted-foreground/50 hover:text-foreground/80",
                )}
              >
                <tab.icon
                  className={cn(
                    "h-3.5 w-3.5 opacity-40 group-hover:opacity-100",
                    activeViewId === tab.id && "opacity-100",
                  )}
                />
                {tab.name}
              </button>
            ))}
            <Button
              variant="ghost"
              size="icon"
              className="h-6 w-6 ml-2 text-muted-foreground/20 hover:text-primary transition-colors"
            >
              <Plus className="h-3.5 w-3.5" />
            </Button>
          </div>

          <div className="ml-auto flex items-center gap-2">
            <div className="flex items-center gap-2 px-3 h-7 rounded-md bg-muted/30 border border-border">
              <Search className="h-3 w-3 text-muted-foreground/40" />
              <span className="text-[10px] font-medium text-muted-foreground/30">
                Search view...
              </span>
            </div>
            <Button
              variant="ghost"
              size="icon"
              className="h-7 w-7 text-muted-foreground/40 hover:text-foreground border border-transparent hover:border-border"
            >
              <Settings2 className="h-3.5 w-3.5" />
            </Button>
            <Button
              variant="ghost"
              size="icon"
              className={cn(
                "h-7 w-7 transition-colors rounded-sm",
                isPropsOpen
                  ? "text-foreground bg-muted/60"
                  : "text-muted-foreground/40 hover:text-foreground",
              )}
              onClick={() => setIsPropsOpen(!isPropsOpen)}
            >
              {isPropsOpen ? (
                <PanelRightClose className="h-3.5 w-3.5" />
              ) : (
                <PanelRight className="h-3.5 w-3.5" />
              )}
            </Button>
          </div>
        </div>

        {/* OPTIONS BAR */}
        <div className="flex items-center gap-3 px-6 h-9 flex-shrink-0 bg-background border-b border-border text-[10px] font-bold text-muted-foreground/60 uppercase tracking-tighter">
          <button className="flex items-center gap-1.5 hover:text-foreground transition-colors">
            <Filter className="h-3 w-3" /> Filter
          </button>
          <button className="flex items-center gap-1.5 hover:text-foreground transition-colors">
            <ArrowDownUp className="h-3 w-3" /> Sort
          </button>
          <div className="w-px h-3 bg-border mx-1" />
          <button className="flex items-center gap-1.5 hover:text-foreground transition-colors">
            <GroupIcon className="h-3 w-3" /> Group By: Status
          </button>

          <Button
            size="sm"
            className="ml-auto h-6 text-[10px] font-black tracking-widest px-4 rounded-sm bg-primary hover:bg-primary/90 text-primary-foreground shadow-sm"
          >
            NEW TASK
          </Button>
        </div>

        {/* VIEW AREA */}
        <div className="flex-1 min-h-0 overflow-hidden">
          <ViewContainer
            workspaceId={workspaceId}
            layerId={entityId}
            layerType={layerType}
            data={activeData}
            view={activeViewDto as any}
          />
        </div>
      </div>

      {/* PROPS HOLDER */}
      <div
        className={cn(
          "flex flex-col h-full bg-background border border-border rounded-xl shadow-sm transition-all duration-300 ease-in-out",
          isPropsOpen
            ? "w-[300px]"
            : "w-0 opacity-0 overflow-hidden pointer-events-none border-none",
        )}
      >
        {/* HORIZONTAL TABS EDGE */}
        <div className="w-full flex items-center p-2 gap-2 border-b border-border flex-shrink-0">
          <TabButton
            icon={Info}
            active={activeTab === "info"}
            onClick={() => {
              setActiveTab("info");
              setIsPropsOpen(true);
            }}
          />
          <TabButton
            icon={MessageSquare}
            active={activeTab === "comments"}
            onClick={() => {
              setActiveTab("comments");
              setIsPropsOpen(true);
            }}
          />
          <TabButton
            icon={Paperclip}
            active={activeTab === "attachments"}
            onClick={() => {
              setActiveTab("attachments");
              setIsPropsOpen(true);
            }}
          />
        </div>

        {/* PROPS CONTENT */}
        <div
          className={cn(
            "flex-1 flex flex-col min-w-0 transition-opacity duration-200 overflow-hidden",
            !isPropsOpen && "opacity-0 pointer-events-none",
          )}
        >
          <div className="h-10 px-4 flex items-center justify-between border-b border-border">
            <span className="text-[10px] font-black uppercase tracking-widest text-muted-foreground/60">
              {activeTab}
            </span>
            <Maximize2 className="h-3 w-3 text-muted-foreground/20 hover:text-foreground cursor-pointer transition-colors" />
          </div>
          <ScrollArea className="flex-1">
            <div className="p-3 space-y-6">
              <div className="space-y-3">
                <div className="h-3 w-32 bg-muted rounded-full" />
                <div className="space-y-1.5">
                  <div className="h-2 w-full bg-muted/50 rounded-full" />
                  <div className="h-2 w-full bg-muted/50 rounded-full" />
                  <div className="h-2 w-3/4 bg-muted/30 rounded-full" />
                </div>
              </div>

              <div className="space-y-4">
                <span className="text-[10px] font-black text-muted-foreground/40 uppercase tracking-widest block border-b border-border pb-2">
                  Properties
                </span>

                {/* Layer-Specific Props Holder */}
                {layerType === EntityLayerType.ProjectSpace && (
                  <SpaceProps entityId={entityId} />
                )}
                {layerType === EntityLayerType.ProjectFolder && (
                  <FolderProps entityId={entityId} />
                )}
                {layerType === EntityLayerType.ProjectTask && (
                  <TaskProps entityId={entityId} />
                )}

                <div className="space-y-4 pt-4 border-t border-border mt-4">
                  <MetaRow label="Owner" value="Nguyen Anh Duc" />
                  <MetaRow label="Layer" value={entityInfo.type} />
                  <MetaRow label="Created" value="Apr 04, 2026" />
                  <MetaRow
                    label="ID"
                    value={entityId.slice(0, 8).toUpperCase()}
                    isMono
                  />
                </div>
              </div>

              <div className="pt-4">
                <Button
                  variant="outline"
                  size="sm"
                  className="w-full h-8 text-[10px] font-black tracking-widest border-border bg-muted/20 hover:bg-muted/40 uppercase"
                >
                  Manage Access
                </Button>
              </div>
            </div>
          </ScrollArea>
        </div>
      </div>
    </div>
  );
}

function TabButton({
  icon: Icon,
  active,
  onClick,
}: {
  icon: any;
  active: boolean;
  onClick: () => void;
}) {
  return (
    <button
      onClick={onClick}
      className={cn(
        "p-2 rounded-sm transition-all duration-200 group relative",
        active
          ? "text-primary bg-primary/10 shadow-[0_0_15px_rgba(var(--primary-rgb),0.1)]"
          : "text-muted-foreground/30 hover:text-muted-foreground/60 hover:bg-white/5",
      )}
    >
      <Icon className="h-3.5 w-3.5" />
      {active && (
        <div className="absolute right-0 top-1/2 -translate-y-1/2 w-0.5 h-4 bg-primary rounded-l-full shadow-lg" />
      )}
    </button>
  );
}

function MetaRow({
  label,
  value,
  color,
  isMono,
}: {
  label: string;
  value: string;
  color?: string;
  isMono?: boolean;
}) {
  return (
    <div className="flex items-center justify-between group">
      <span className="text-[10px] font-medium text-muted-foreground/40">
        {label}
      </span>
      <span
        className={cn(
          "text-[11px] font-bold text-muted-foreground/70 group-hover:text-foreground transition-colors",
          isMono && "font-mono opacity-50",
        )}
        style={{ color }}
      >
        {value}
      </span>
    </div>
  );
}
