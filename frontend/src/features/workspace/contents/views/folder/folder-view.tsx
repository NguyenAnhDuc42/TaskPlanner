import { EntityViewFrame } from "../entity-view-frame";
import { FolderTaskList } from "./components/folder-task-list";
import { Folder, Trash2, MoreHorizontal, LayoutGrid, GitMerge, Calendar } from "lucide-react";
import { TaskDetailCanvas } from "../task/components/task-detail-canvas";
import * as React from "react";
import { useParams, Link } from "@tanstack/react-router";
import { format } from "date-fns";
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "@/components/ui/breadcrumb";
import { Button } from "@/components/ui/button";
import { PriorityBadge } from "@/components/priority-badge";
import { DateBadge } from "@/components/date-badge";
import {
  useGetFolderDetailQuery,
  useFolderDetail,
  useUpdateFolderFieldMutation,
} from "./folder-api";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { Calendar as ShadcnCalendar } from "@/components/ui/calendar";
import { FolderTaskBatchBar } from "./components/folder-task-batch-bar";
import { DynamicIcon } from "@/components/dynamic-icon";
import { Priority } from "@/types/priority";
import { PopoverFormWrapper } from "@/components/popover-wrapper";
import { UniversalPicker } from "@/components/universal-picker";
import { Input } from "@/components/ui/input";
import { CreateStatusForm } from "@/features/workspace/components/forms/create-status-form";
import type { FolderRecord } from "@/types/projects/folder-record";

export function FolderView() {
  const { folderId, workspaceId } = useParams({ strict: false }) as {
    folderId: string;
    workspaceId: string;
  };

  const [checkedTaskIds, setCheckedTaskIds] = React.useState<Set<string>>(new Set());
  const [isWorkflowOpen, setIsWorkflowOpen] = React.useState(false);

  // Load folder + statuses into Redux (single source of truth)
  const { data: detailData, isLoading } = useGetFolderDetailQuery(folderId);

  // Read folder reactively from Redux (real-time safe)
  const folder = useFolderDetail(folderId);

  const [updateFolderField] = useUpdateFolderFieldMutation();

  const updateField = (patches: Partial<FolderRecord>) => {
    updateFolderField({ folderId, patches });
  };

  const toggleCheck = (id: string, e: React.MouseEvent) => {
    e.stopPropagation();
    if (id === "clear_all") {
      setCheckedTaskIds(new Set());
      return;
    }
    setCheckedTaskIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  if (isLoading) {
    return <div className="p-8 text-sm text-muted-foreground animate-pulse">Loading folder...</div>;
  }

  return (
    <EntityViewFrame
      topHeader={
        <div className="flex items-center justify-between w-full">
          <Breadcrumb className="text-xs">
            <BreadcrumbList className="text-xs sm:gap-1.5">
              <BreadcrumbItem>
                <BreadcrumbLink asChild>
                  <Link
                    to="/workspaces/$workspaceId/spaces/$spaceId"
                    params={{ workspaceId, spaceId: "mock-space" }}
                    className="flex items-center gap-1.5 text-muted-foreground hover:text-foreground"
                  >
                    <LayoutGrid className="h-3 w-3" />
                    Engineering
                  </Link>
                </BreadcrumbLink>
              </BreadcrumbItem>
              <BreadcrumbSeparator className="[&>svg]:w-3 [&>svg]:h-3" />
              <BreadcrumbItem>
                <BreadcrumbPage className="font-medium text-foreground flex items-center gap-1.5">
                  {folder?.name ?? "Folder"}
                </BreadcrumbPage>
              </BreadcrumbItem>
            </BreadcrumbList>
          </Breadcrumb>

          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="h-7 w-7 text-muted-foreground">
                <MoreHorizontal className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem className="text-destructive focus:text-destructive focus:bg-destructive/10">
                <Trash2 className="h-4 w-4 mr-2" />
                Delete Folder
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      }
    >
      <div className="h-full w-full flex flex-col bg-background/25 p-2 gap-2 overflow-hidden relative">
        {/* Ambient background accent glow */}
        <div 
          className="absolute right-12 bottom-12 w-[350px] h-[350px] rounded-full blur-[120px] opacity-[0.05] pointer-events-none transition-all duration-700"
          style={{ backgroundColor: folder?.color || "#6366f1" }}
        />

        {/* Integrated Floating Folder Header Bar */}
        <div className="flex items-center justify-between px-2.5 py-1 rounded-md border border-border/30 bg-card/30 backdrop-blur-md shadow-sm shrink-0">
          <div className="flex items-center gap-1.5">
            <PopoverFormWrapper
              trigger={
                <button className="flex items-center justify-center p-0.5 hover:bg-muted/65 rounded-md transition-all cursor-pointer focus:outline-none border border-border/10 shadow-sm bg-background/80">
                  {folder?.icon ? (
                    <DynamicIcon name={folder.icon} className="h-3 w-3" color={folder.color} />
                  ) : (
                    <Folder className="h-3 w-3" color={folder?.color} />
                  )}
                </button>
              }
            >
              <UniversalPicker
                selectedIcon={folder?.icon ?? "Folder"}
                selectedColor={folder?.color ?? "#6366f1"}
                onSelect={(icon, color) => updateField({ icon, color })}
              />
            </PopoverFormWrapper>

            <input
              className="h-6 px-1 w-56 text-xs font-bold text-foreground/90 tracking-tight bg-transparent border-none outline-none hover:bg-muted/20 focus:bg-muted/40 transition-all rounded cursor-text"
              defaultValue={folder?.name ?? "Folder"}
              onBlur={(e) => {
                if (e.target.value && e.target.value !== folder?.name) {
                  updateField({ name: e.target.value });
                }
              }}
              onKeyDown={(e) => {
                if (e.key === "Enter") e.currentTarget.blur();
              }}
            />
          </div>

          <div className="flex items-center gap-2">
            <DropdownMenu>
              <DropdownMenuTrigger className="focus:outline-none rounded-md">
                <PriorityBadge 
                  priority={folder?.priority} 
                  className="h-5 px-2 rounded-md cursor-pointer border border-border/10 shadow-sm"
                />
              </DropdownMenuTrigger>
              <DropdownMenuContent>
                {Object.values(Priority).map((p) => (
                  <DropdownMenuItem key={p} onClick={() => updateField({ priority: p })}>
                    <PriorityBadge priority={p} />
                  </DropdownMenuItem>
                ))}
              </DropdownMenuContent>
            </DropdownMenu>

            {/* Start Date */}
            <Popover>
              <PopoverTrigger asChild>
                <button className="focus:outline-none flex items-center h-5 rounded-md transition-all cursor-pointer border border-border/10 shadow-sm text-[10px] font-semibold shrink-0 bg-muted/40 hover:bg-muted/75 hover:text-foreground text-muted-foreground gap-1.5 px-2">
                  <Calendar className="h-3 w-3 opacity-80" />
                  <span>
                    {folder?.startDate ? (
                      `Start: ${format(new Date(folder.startDate), "MMM d, yyyy")}`
                    ) : (
                      "Set Start"
                    )}
                  </span>
                </button>
              </PopoverTrigger>
              <PopoverContent className="w-auto p-0 border-border/40 shadow-2xl rounded-xl" align="end">
                <ShadcnCalendar
                  mode="single"
                  selected={folder?.startDate ? new Date(folder.startDate) : undefined}
                  onSelect={(date) => updateField({ startDate: date?.toISOString() })}
                  initialFocus
                />
              </PopoverContent>
            </Popover>

            {/* Due Date */}
            <Popover>
              <PopoverTrigger asChild>
                <button className="focus:outline-none flex items-center h-5 rounded-md transition-all cursor-pointer border border-border/10 shadow-sm text-[10px] font-semibold shrink-0 bg-muted/40 hover:bg-muted/75 hover:text-foreground text-muted-foreground gap-1.5 px-2">
                  <Calendar className="h-3 w-3 opacity-80" />
                  <span>
                    {folder?.dueDate ? (
                      `Due: ${format(new Date(folder.dueDate), "MMM d, yyyy")}`
                    ) : (
                      "Set Due"
                    )}
                  </span>
                </button>
              </PopoverTrigger>
              <PopoverContent className="w-auto p-0 border-border/40 shadow-2xl rounded-xl" align="end">
                <ShadcnCalendar
                  mode="single"
                  selected={folder?.dueDate ? new Date(folder.dueDate) : undefined}
                  onSelect={(date) => updateField({ dueDate: date?.toISOString() })}
                  initialFocus
                />
              </PopoverContent>
            </Popover>

            <button
              className="flex items-center h-5 gap-1.5 px-2.5 rounded-md bg-muted/45 text-[10px] text-muted-foreground font-semibold hover:bg-muted hover:text-foreground transition-all cursor-pointer border border-border/10 shadow-sm"
              onClick={() => setIsWorkflowOpen(true)}
            >
              <GitMerge className="h-3 w-3 opacity-80" />
              <span>Workflow</span>
            </button>

            {folder?.createdAt && (
              <div className="text-[10px] text-muted-foreground/50 pl-2 border-l border-border/20">
                Created {new Date(folder.createdAt).toLocaleDateString()}
              </div>
            )}
          </div>
        </div>

        {/* Floating Content Area */}
        <div className="flex-1 flex gap-2 overflow-hidden relative">
          {/* Left Card: Folder Task List Column */}
          <div className="w-[280px] rounded-md border border-border/40 bg-card/35 backdrop-blur-md shadow-sm overflow-hidden flex flex-col shrink-0">
            <FolderTaskList
              checkedTaskIds={checkedTaskIds}
              onToggleCheck={toggleCheck}
            />
          </div>

          {/* Right Card: Task Detail Canvas */}
          <div className="flex-1 rounded-md border border-border/40 bg-card/35 backdrop-blur-md shadow-sm overflow-hidden flex flex-col">
            <TaskDetailCanvas taskId="1" />
          </div>
        </div>
      </div>

      <CreateStatusForm
        isOpen={isWorkflowOpen}
        onClose={() => setIsWorkflowOpen(false)}
        workflowId={detailData?.workflowId}
        currentStatuses={detailData?.statuses}
      />

      {checkedTaskIds.size > 0 && (
        <FolderTaskBatchBar
          folderId={folderId}
          checkedTaskIds={checkedTaskIds}
          onClear={() => setCheckedTaskIds(new Set())}
        />
      )}
    </EntityViewFrame>
  );
}
