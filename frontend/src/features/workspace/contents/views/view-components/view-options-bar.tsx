import type { DisplayConfig, ViewDto } from "../views-type";
import { useUpdateView } from "../views-api";
import { CreateViewForm } from "./create-view-form/create-view-form";
import { CreateTaskButton } from "../../tasks/tasks-component/create-task-button";
import { Filter, ArrowDownUp, Group, Columns3, Settings2 } from "lucide-react";
import { EntityLayerType } from "@/types/relationship-type";
import {
  DropdownMenu,
  DropdownMenuCheckboxItem,
  DropdownMenuContent,
  DropdownMenuLabel,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";

interface ViewOptionsBarProps {
  view: ViewDto;
  layerId: string;
  layerType: EntityLayerType;
  workspaceId: string;
}

const COLUMNS = [
  { id: "assignee", label: "Assignee" },
  { id: "dueDate", label: "Due Date" },
  { id: "priority", label: "Priority" },
];

export function ViewOptionsBar({
  view,
  layerId,
  layerType,
  workspaceId,
}: ViewOptionsBarProps) {
  const updateView = useUpdateView();

  const displayConfig: DisplayConfig = view.displayConfigJson
    ? JSON.parse(view.displayConfigJson)
    : {
        groupBy: "status",
        visibleColumns: ["assignee", "dueDate", "priority"],
      };

  const handleUpdateDisplayConfig = (newConfig: Partial<DisplayConfig>) => {
    const updatedConfig = { ...displayConfig, ...newConfig };
    updateView.mutate({
      id: view.id,
      layerId,
      layerType,
      displayConfigJson: JSON.stringify(updatedConfig),
    });
  };

  const visibleColumns = displayConfig.visibleColumns || [
    "assignee",
    "dueDate",
    "priority",
  ];

  const toggleColumn = (colId: string) => {
    if (visibleColumns.includes(colId)) {
      handleUpdateDisplayConfig({
        visibleColumns: visibleColumns.filter((c) => c !== colId),
      });
    } else {
      handleUpdateDisplayConfig({ visibleColumns: [...visibleColumns, colId] });
    }
  };

  return (
    <div className="flex items-center gap-2 px-6 h-[48px] bg-background/10 backdrop-blur-sm relative z-10 border-b border-white/5">
      {/* Filter */}
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <div className="flex items-center gap-2 px-3 py-1.5 rounded-lg hover:bg-white/5 cursor-pointer transition-all group">
            <Filter className="h-3.5 w-3.5 text-muted-foreground/40 group-hover:text-foreground" />
            <span className="text-[10px] font-black uppercase tracking-widest text-muted-foreground/40 group-hover:text-foreground">Filter</span>
          </div>
        </DropdownMenuTrigger>
        <DropdownMenuContent
          align="start"
          className="w-64 p-1.5 rounded-xl border-white/10 bg-card/40 backdrop-blur-2xl shadow-2xl"
        >
          <DropdownMenuLabel className="px-3 py-2 text-[9px] font-black text-muted-foreground/40 uppercase tracking-[0.3em]">
            Filter Parameters
          </DropdownMenuLabel>
          <DropdownMenuSeparator className="bg-white/5 mx-2" />
          <div className="p-4 text-[10px] text-muted-foreground/30 text-center italic font-bold uppercase tracking-widest">
            Advanced logic pending
          </div>
        </DropdownMenuContent>
      </DropdownMenu>

      {/* Sort */}
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <div className="flex items-center gap-2 px-3 py-1.5 rounded-lg hover:bg-white/5 cursor-pointer transition-all group">
            <ArrowDownUp className="h-3.5 w-3.5 text-muted-foreground/40 group-hover:text-foreground" />
            <span className="text-[10px] font-black uppercase tracking-widest text-muted-foreground/40 group-hover:text-foreground">Sort</span>
          </div>
        </DropdownMenuTrigger>
        <DropdownMenuContent
          align="start"
          className="w-56 p-1.5 rounded-xl border-white/10 bg-card/40 backdrop-blur-2xl shadow-2xl"
        >
          <DropdownMenuLabel className="px-3 py-2 text-[9px] font-black text-muted-foreground/40 uppercase tracking-[0.3em]">
            Ordering
          </DropdownMenuLabel>
          <DropdownMenuSeparator className="bg-white/5 mx-2" />
          <DropdownMenuRadioGroup
            value={displayConfig.sortBy || "createdAt"}
            onValueChange={(val) => handleUpdateDisplayConfig({ sortBy: val })}
          >
            <DropdownMenuRadioItem value="createdAt" className="rounded-lg text-[11px] font-bold uppercase tracking-wider py-2.5">
              Launch Sequence
            </DropdownMenuRadioItem>
            <DropdownMenuRadioItem value="dueDate" className="rounded-lg text-[11px] font-bold uppercase tracking-wider py-2.5">
              Deadline
            </DropdownMenuRadioItem>
            <DropdownMenuRadioItem value="priority" className="rounded-lg text-[11px] font-bold uppercase tracking-wider py-2.5">
              Urgency
            </DropdownMenuRadioItem>
          </DropdownMenuRadioGroup>
        </DropdownMenuContent>
      </DropdownMenu>

      <div className="w-px h-4 bg-white/5 mx-1" />

      {/* Group By */}
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <div className="flex items-center gap-2 px-3 py-1.5 rounded-lg hover:bg-white/5 cursor-pointer transition-all group">
            <Group className="h-3.5 w-3.5 text-muted-foreground/40 group-hover:text-foreground" />
            <span className="text-[10px] font-black uppercase tracking-widest text-muted-foreground/40 group-hover:text-foreground">Group:</span>
            <span className="text-[10px] font-black uppercase tracking-widest text-primary">
              {displayConfig.groupBy || "None"}
            </span>
          </div>
        </DropdownMenuTrigger>
        <DropdownMenuContent
          align="start"
          className="w-56 p-1.5 rounded-xl border-white/10 bg-card/40 backdrop-blur-2xl shadow-2xl"
        >
          <DropdownMenuLabel className="px-3 py-2 text-[9px] font-black text-muted-foreground/40 uppercase tracking-[0.3em]">
            Categorization
          </DropdownMenuLabel>
          <DropdownMenuSeparator className="bg-white/5 mx-2" />
          <DropdownMenuRadioGroup
            value={displayConfig.groupBy || "none"}
            onValueChange={(val) =>
              handleUpdateDisplayConfig({
                groupBy: val === "none" ? undefined : val,
              })
            }
          >
            <DropdownMenuRadioItem value="status" className="rounded-lg text-[11px] font-bold uppercase tracking-wider py-2.5">
              Status Array
            </DropdownMenuRadioItem>
            <DropdownMenuRadioItem value="none" className="rounded-lg text-[11px] font-bold uppercase tracking-wider py-2.5">
              None
            </DropdownMenuRadioItem>
          </DropdownMenuRadioGroup>
        </DropdownMenuContent>
      </DropdownMenu>

      {/* Columns */}
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <div className="flex items-center gap-2 px-3 py-1.5 rounded-lg hover:bg-white/5 cursor-pointer transition-all group">
            <Columns3 className="h-3.5 w-3.5 text-muted-foreground/40 group-hover:text-foreground" />
            <span className="text-[10px] font-black uppercase tracking-widest text-muted-foreground/40 group-hover:text-foreground">Columns</span>
          </div>
        </DropdownMenuTrigger>
        <DropdownMenuContent
          align="start"
          className="w-64 p-1.5 rounded-xl border-white/10 bg-card/40 backdrop-blur-2xl shadow-2xl"
        >
          <DropdownMenuLabel className="px-3 py-2 text-[9px] font-black text-muted-foreground/40 uppercase tracking-[0.3em]">
            Visible Metrics
          </DropdownMenuLabel>
          <DropdownMenuSeparator className="bg-white/5 mx-2" />
          <DropdownMenuCheckboxItem checked disabled className="rounded-lg text-[11px] font-bold uppercase tracking-wider py-2.5">
            Objective Name
          </DropdownMenuCheckboxItem>
          {COLUMNS.map((col) => (
            <DropdownMenuCheckboxItem
              key={col.id}
              className="rounded-lg text-[11px] font-bold uppercase tracking-wider py-2.5"
              checked={visibleColumns.includes(col.id)}
              onCheckedChange={() => toggleColumn(col.id)}
            >
              {col.label}
            </DropdownMenuCheckboxItem>
          ))}
        </DropdownMenuContent>
      </DropdownMenu>

      <div className="ml-auto flex items-center gap-5">
        <CreateTaskButton
          workspaceId={workspaceId}
          layerId={layerId}
          layerType={layerType}
        />
        <CreateViewForm layerId={layerId} layerType={layerType} />
        
        <div className="flex items-center gap-2 group cursor-pointer">
          <Settings2 className="h-3.5 w-3.5 text-muted-foreground/40 group-hover:text-foreground transition-colors" />
          <span className="text-[10px] uppercase tracking-[0.2em] font-black text-muted-foreground/40 group-hover:text-foreground transition-all">
            MANAGE
          </span>
        </div>
      </div>
    </div>
  );
}
