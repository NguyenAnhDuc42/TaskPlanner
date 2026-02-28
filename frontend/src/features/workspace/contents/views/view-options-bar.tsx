import type { DisplayConfig, ViewDto } from "./views-type";
import { useUpdateView } from "./views-api";
import { CreateViewForm } from "./view-components/create-view-form";
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
    <div className="flex items-center gap-2 px-4 py-2 border-b bg-background text-sm text-muted-foreground">
      {/* Filter (Shell for now) */}
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button
            variant="ghost"
            size="sm"
            className="h-7 text-xs gap-1.5 px-2"
          >
            <Filter className="h-3 w-3" />
            Filter
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start" className="w-56">
          <DropdownMenuLabel>Filters</DropdownMenuLabel>
          <DropdownMenuSeparator />
          <div className="p-2 text-xs text-muted-foreground text-center italic">
            Advanced filtering UI coming soon
          </div>
          <DropdownMenuSeparator />
          <DropdownMenuCheckboxItem disabled>
            Only my tasks
          </DropdownMenuCheckboxItem>
          <DropdownMenuCheckboxItem disabled>
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
            className="h-7 text-xs gap-1.5 px-2"
          >
            <ArrowDownUp className="h-3 w-3" />
            Sort
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start">
          <DropdownMenuLabel>Sort By</DropdownMenuLabel>
          <DropdownMenuSeparator />
          <DropdownMenuRadioGroup
            value={displayConfig.sortBy || "createdAt"}
            onValueChange={(val) => handleUpdateDisplayConfig({ sortBy: val })}
          >
            <DropdownMenuRadioItem value="createdAt">
              Created Date
            </DropdownMenuRadioItem>
            <DropdownMenuRadioItem value="dueDate">
              Due Date
            </DropdownMenuRadioItem>
            <DropdownMenuRadioItem value="priority">
              Priority
            </DropdownMenuRadioItem>
          </DropdownMenuRadioGroup>
        </DropdownMenuContent>
      </DropdownMenu>

      <div className="w-px h-4 bg-border mx-1" />

      {/* Group By */}
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button
            variant="ghost"
            size="sm"
            className="h-7 text-xs gap-1.5 px-2"
          >
            <Group className="h-3 w-3" />
            Group:{" "}
            <span className="text-foreground capitalize">
              {displayConfig.groupBy || "None"}
            </span>
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start">
          <DropdownMenuLabel>Group By</DropdownMenuLabel>
          <DropdownMenuSeparator />
          <DropdownMenuRadioGroup
            value={displayConfig.groupBy || "none"}
            onValueChange={(val) =>
              handleUpdateDisplayConfig({
                groupBy: val === "none" ? undefined : val,
              })
            }
          >
            <DropdownMenuRadioItem value="status">Status</DropdownMenuRadioItem>
            <DropdownMenuRadioItem value="priority" disabled>
              Priority (Pro)
            </DropdownMenuRadioItem>
            <DropdownMenuRadioItem value="assignee" disabled>
              Assignee (Pro)
            </DropdownMenuRadioItem>
            <DropdownMenuRadioItem value="none">None</DropdownMenuRadioItem>
          </DropdownMenuRadioGroup>
        </DropdownMenuContent>
      </DropdownMenu>

      {/* Columns */}
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button
            variant="ghost"
            size="sm"
            className="h-7 text-xs gap-1.5 px-2"
          >
            <Columns3 className="h-3 w-3" />
            Columns
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start" className="w-48">
          <DropdownMenuLabel>Visible Columns</DropdownMenuLabel>
          <DropdownMenuSeparator />
          <DropdownMenuCheckboxItem checked disabled>
            Task Name
          </DropdownMenuCheckboxItem>
          {COLUMNS.map((col) => (
            <DropdownMenuCheckboxItem
              key={col.id}
              checked={visibleColumns.includes(col.id)}
              onCheckedChange={() => toggleColumn(col.id)}
            >
              {col.label}
            </DropdownMenuCheckboxItem>
          ))}
        </DropdownMenuContent>
      </DropdownMenu>

      <div className="ml-auto flex items-center gap-2">
        <CreateViewForm layerId={layerId} layerType={layerType} />
        <Button variant="ghost" size="sm" className="h-7 text-xs gap-1.5">
          <Settings2 className="h-3 w-3" />
          Customize
        </Button>
      </div>
    </div>
  );
}
