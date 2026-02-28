import type { DisplayConfig, ViewDto } from "./views-type";
import { useUpdateView } from "./views-api";
import { CreateViewForm } from "./view-components/create-view-form/create-view-form";
import { Button } from "@/components/ui/button";
import { Filter, ArrowDownUp, Group, Columns3, Settings2 } from "lucide-react";
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
  layerType: string;
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
    <div className="flex items-center gap-1.5 px-6 py-2.5 border-b bg-muted/5 text-sm text-muted-foreground/80 backdrop-blur-sm relative z-10">
      {/* Filter (Shell for now) */}
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button
            variant="ghost"
            size="sm"
            className="h-8 text-[12px] gap-2 px-3 hover:bg-muted font-semibold transition-all"
          >
            <Filter className="h-3.5 w-3.5" />
            Filter
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent
          align="start"
          className="w-56 p-1.5 rounded-xl border-muted-foreground/10"
        >
          <DropdownMenuLabel className="text-xs uppercase tracking-widest text-muted-foreground font-bold">
            Filters
          </DropdownMenuLabel>
          <DropdownMenuSeparator className="my-1" />
          <div className="p-4 text-xs text-muted-foreground text-center italic font-medium bg-muted/30 rounded-lg mx-1 my-1">
            Advanced filtering UI coming soon
          </div>
          <DropdownMenuSeparator className="my-1" />
          <DropdownMenuCheckboxItem disabled className="rounded-lg">
            Only my tasks
          </DropdownMenuCheckboxItem>
          <DropdownMenuCheckboxItem disabled className="rounded-lg">
            Due this week
          </DropdownMenuCheckboxItem>
        </DropdownMenuContent>
      </DropdownMenu>

      {/* Sort (Shell for now) */}
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button
            variant="ghost"
            size="sm"
            className="h-8 text-[12px] gap-2 px-3 hover:bg-muted font-semibold transition-all"
          >
            <ArrowDownUp className="h-3.5 w-3.5" />
            Sort
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent
          align="start"
          className="w-48 p-1.5 rounded-xl border-muted-foreground/10"
        >
          <DropdownMenuLabel className="text-xs uppercase tracking-widest text-muted-foreground font-bold">
            Sort By
          </DropdownMenuLabel>
          <DropdownMenuSeparator className="my-1" />
          <DropdownMenuRadioGroup
            value={displayConfig.sortBy || "createdAt"}
            onValueChange={(val) => handleUpdateDisplayConfig({ sortBy: val })}
          >
            <DropdownMenuRadioItem value="createdAt" className="rounded-lg">
              Created Date
            </DropdownMenuRadioItem>
            <DropdownMenuRadioItem value="dueDate" className="rounded-lg">
              Due Date
            </DropdownMenuRadioItem>
            <DropdownMenuRadioItem value="priority" className="rounded-lg">
              Priority
            </DropdownMenuRadioItem>
          </DropdownMenuRadioGroup>
        </DropdownMenuContent>
      </DropdownMenu>

      <div className="w-px h-4 bg-muted-foreground/20 mx-1.5" />

      {/* Group By */}
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button
            variant="ghost"
            size="sm"
            className="h-8 text-[12px] gap-2 px-3 hover:bg-muted font-semibold transition-all"
          >
            <Group className="h-3.5 w-3.5" />
            <span className="opacity-70">Group:</span>{" "}
            <span className="text-foreground/90 capitalize font-bold">
              {displayConfig.groupBy || "None"}
            </span>
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent
          align="start"
          className="w-48 p-1.5 rounded-xl border-muted-foreground/10"
        >
          <DropdownMenuLabel className="text-xs uppercase tracking-widest text-muted-foreground font-bold">
            Group By
          </DropdownMenuLabel>
          <DropdownMenuSeparator className="my-1" />
          <DropdownMenuRadioGroup
            value={displayConfig.groupBy || "none"}
            onValueChange={(val) =>
              handleUpdateDisplayConfig({
                groupBy: val === "none" ? undefined : val,
              })
            }
          >
            <DropdownMenuRadioItem value="status" className="rounded-lg">
              Status
            </DropdownMenuRadioItem>
            <DropdownMenuRadioItem
              value="priority"
              disabled
              className="rounded-lg"
            >
              Priority (Pro)
            </DropdownMenuRadioItem>
            <DropdownMenuRadioItem
              value="assignee"
              disabled
              className="rounded-lg"
            >
              Assignee (Pro)
            </DropdownMenuRadioItem>
            <DropdownMenuRadioItem value="none" className="rounded-lg">
              None
            </DropdownMenuRadioItem>
          </DropdownMenuRadioGroup>
        </DropdownMenuContent>
      </DropdownMenu>

      {/* Columns */}
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button
            variant="ghost"
            size="sm"
            className="h-8 text-[12px] gap-2 px-3 hover:bg-muted font-semibold transition-all"
          >
            <Columns3 className="h-3.5 w-3.5" />
            Columns
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent
          align="start"
          className="w-56 p-1.5 rounded-xl border-muted-foreground/10"
        >
          <DropdownMenuLabel className="text-xs uppercase tracking-widest text-muted-foreground font-bold">
            Visible Columns
          </DropdownMenuLabel>
          <DropdownMenuSeparator className="my-1" />
          <DropdownMenuCheckboxItem checked disabled className="rounded-lg">
            Task Name
          </DropdownMenuCheckboxItem>
          {COLUMNS.map((col) => (
            <DropdownMenuCheckboxItem
              key={col.id}
              className="rounded-lg"
              checked={visibleColumns.includes(col.id)}
              onCheckedChange={() => toggleColumn(col.id)}
            >
              {col.label}
            </DropdownMenuCheckboxItem>
          ))}
        </DropdownMenuContent>
      </DropdownMenu>

      <div className="ml-auto flex items-center gap-3">
        <CreateViewForm layerId={layerId} layerType={layerType} />
        <Button
          variant="ghost"
          size="sm"
          className="h-8 text-[12px] gap-2 px-3 hover:bg-muted font-bold transition-all"
        >
          <Settings2 className="h-3.5 w-3.5" />
          CUSTOMIZE
        </Button>
      </div>
    </div>
  );
}
