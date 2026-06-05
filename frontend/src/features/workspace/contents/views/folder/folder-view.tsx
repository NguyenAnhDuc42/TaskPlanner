import { EntityViewFrame } from "../entity-view-frame";
import { FolderTaskList } from "./components/folder-task-list";
import { Folder, Trash2, MoreHorizontal, GitMerge, Calendar, Circle } from "lucide-react";
import { TaskDetailCanvas } from "../task/components/task-detail-canvas";
import * as React from "react";
import { useParams, Link } from "@tanstack/react-router";
import { format } from "date-fns";
import { useSpaceDetail, useGetSpaceDetailQuery, useSpaceStatuses } from "../space/space-api";
import { DynamicIcon } from "@/components/dynamic-icon";
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
import { StatusBadge } from "@/components/status-badge";
import { StatusSelect } from "@/components/status-select";
import { PrioritySelect } from "@/components/priority-select";
import { AttributeButton } from "@/features/workspace/components/forms/form-elements";
import { useSelector } from "react-redux";
import { statusSelectors } from "@/store/entityStore";
import {
  useGetFolderDetailQuery,
  useFolderDetail,
  useUpdateFolderFieldMutation,
  useFolderTasksList,
} from "./folder-api";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { DateSelect } from "@/components/date-select";
import { FolderTaskBatchBar } from "./components/folder-task-batch-bar";
import { PopoverFormWrapper } from "@/components/popover-wrapper";
import { UniversalPicker } from "@/components/universal-picker";
import { CreateStatusForm } from "@/features/workspace/components/forms/create-status-form";
import type { FolderRecord } from "@/types/projects/folder-record";

export function FolderView() {
  const { folderId, workspaceId } = useParams({ strict: false }) as {
    folderId: string;
    workspaceId: string;
  };

  const [checkedTaskIds, setCheckedTaskIds] = React.useState<Set<string>>(new Set());
  const [isWorkflowOpen, setIsWorkflowOpen] = React.useState(false);
  const [selectedTaskId, setSelectedTaskId] = React.useState<string | undefined>(undefined);

  // Load folder + statuses into Redux (single source of truth)
  const { isLoading } = useGetFolderDetailQuery(folderId);

  // Read folder reactively from Redux (real-time safe)
  const folder = useFolderDetail(folderId);
  const tasks = useFolderTasksList(folderId);
  const parentSpace = useSpaceDetail(folder?.spaceId ?? "");

  // Load parent space details to ensure we always have the workflow of the space above it
  useGetSpaceDetailQuery(folder?.spaceId ?? "", { skip: !folder?.spaceId });

  const [updateFolderField] = useUpdateFolderFieldMutation();

  React.useEffect(() => {
    if (tasks.length > 0) {
      const exists = tasks.some(t => t.id === selectedTaskId);
      if (!selectedTaskId || !exists) {
        setSelectedTaskId(tasks[0].id);
      }
    } else {
      setSelectedTaskId(undefined);
    }
  }, [tasks, selectedTaskId]);

  const allStatuses = useSelector(statusSelectors.selectAll);

  // Retrieve space statuses & folder task statuses directly from Redux
  const spaceStatuses = useSpaceStatuses(folder?.spaceId ?? "");

  const taskStatuses = React.useMemo(() => {
    const targetWorkflowId = folder?.workflowId;
    if (!targetWorkflowId) return [];
    return allStatuses
      .filter(s => s.workflowId?.toLowerCase() === targetWorkflowId.toLowerCase())
      .sort((a, b) => (a.orderKey || "").localeCompare(b.orderKey || ""));
  }, [folder?.workflowId, allStatuses]);

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
              {parentSpace && (
                <>
                  <BreadcrumbItem>
                    <BreadcrumbLink asChild>
                      <Link
                        to="/workspaces/$workspaceId/spaces/$spaceId"
                        params={{ workspaceId, spaceId: folder?.spaceId ?? "" }}
                        className="flex items-center gap-1.5 text-muted-foreground hover:text-foreground transition-colors"
                      >
                        <DynamicIcon
                          name={parentSpace.icon || "Folder"}
                          size={11}
                          color={parentSpace.color || "#3b82f6"}
                          className="stroke-[2.5] shrink-0"
                        />
                        <span>{parentSpace.name}</span>
                      </Link>
                    </BreadcrumbLink>
                  </BreadcrumbItem>
                  <BreadcrumbSeparator className="[&>svg]:w-3 [&>svg]:h-3" />
                </>
              )}
              <BreadcrumbItem>
                <BreadcrumbPage className="font-medium text-foreground flex items-center gap-1.5">
                  <DynamicIcon
                    name={folder?.icon || "Folder"}
                    size={11}
                    color={folder?.color || "#6366f1"}
                    className="stroke-[2.5] shrink-0"
                  />
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
      <div className="h-full w-full flex flex-col bg-background/25 p-1 gap-1 overflow-hidden relative">
        {/* Ambient background accent glow */}
        <div 
          className="absolute right-12 bottom-12 w-[350px] h-[350px] rounded-full blur-[120px] opacity-[0.05] pointer-events-none transition-all duration-700"
          style={{ backgroundColor: folder?.color || "#6366f1" }}
        />

        {/* Integrated Floating Folder Header Bar */}
        <div className="flex items-center justify-between px-1.5 py-1 rounded-md border border-border/30 bg-card/30 backdrop-blur-md shadow-sm shrink-0">
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
            {/* Status Selector */}
            <StatusSelect
              value={folder?.statusId || undefined}
              onChange={(statusId) => updateField({ statusId })}
              workflowId={parentSpace?.workflowId}
              statuses={spaceStatuses}
              align="end"
              trigger={
                <button type="button" className="cursor-pointer focus:outline-none bg-transparent border-none p-0">
                  {folder?.statusId ? (
                    <StatusBadge
                      status={
                        spaceStatuses.find((s) => s.id?.toLowerCase() === folder.statusId?.toLowerCase()) ||
                        allStatuses.find((s) => s.id?.toLowerCase() === folder.statusId?.toLowerCase())
                      }
                      variant="pill"
                    />
                  ) : (
                    <div className="flex items-center h-5 gap-1.5 px-2 rounded-sm bg-muted/50 text-[10px] text-muted-foreground font-semibold hover:bg-muted/80 transition-colors">
                      <Circle className="h-3 w-3 opacity-70" />
                      <span>Status</span>
                    </div>
                  )}
                </button>
              }
            />

            {/* Reusable Priority Selector */}
            <PrioritySelect
              value={folder?.priority}
              onChange={(priority) => updateField({ priority })}
              align="end"
              trigger={
                <button type="button" className="cursor-pointer focus:outline-none bg-transparent border-none p-0">
                  <PriorityBadge priority={folder?.priority} />
                </button>
              }
            />

            <DateSelect
              startDate={folder?.startDate}
              dueDate={folder?.dueDate}
              onStartDateChange={(date) => updateField({ startDate: date?.toISOString() })}
              onDueDateChange={(date) => updateField({ dueDate: date?.toISOString() })}
              align="end"
              size="sm"
              triggerClassName="h-5 px-2 text-[10px] font-semibold rounded-md border border-border/10 bg-muted/40 hover:bg-muted/75 hover:text-foreground text-muted-foreground transition-all cursor-pointer shadow-sm"
            />

            <button
              className="flex items-center h-5 gap-1.5 px-2.5 rounded-md bg-muted/45 text-[10px] text-muted-foreground font-semibold hover:bg-muted hover:text-foreground transition-all cursor-pointer border border-border/10 shadow-sm"
              onClick={() => setIsWorkflowOpen(true)}
            >
              <GitMerge className="h-3 w-3 opacity-80" />
              <span>Workflow</span>
            </button>
          </div>
        </div>

        {/* Floating Content Area */}
        <div className="flex-1 flex gap-1 overflow-hidden relative">
          {/* Left Card: Folder Task List Column */}
          <div className="w-[280px] rounded-md border border-border/40 bg-card/35 backdrop-blur-md shadow-sm overflow-hidden flex flex-col shrink-0">
            <FolderTaskList
              checkedTaskIds={checkedTaskIds}
              onToggleCheck={toggleCheck}
              selectedTaskId={selectedTaskId}
              onSelectTask={setSelectedTaskId}
            />
          </div>

          {/* Right Card: Task Detail Canvas */}
          <div className="flex-1 rounded-md border border-border/40 bg-card/35 backdrop-blur-md shadow-sm overflow-hidden flex flex-col">
            <TaskDetailCanvas taskId={selectedTaskId} />
          </div>
        </div>
      </div>

      <CreateStatusForm
        isOpen={isWorkflowOpen}
        onClose={() => setIsWorkflowOpen(false)}
        workflowId={folder?.workflowId}
      />

      {checkedTaskIds.size > 0 && (
        <FolderTaskBatchBar
          folderId={folderId}
          checkedTaskIds={checkedTaskIds}
          onClear={() => setCheckedTaskIds(new Set())}
          statuses={taskStatuses}
        />
      )}
    </EntityViewFrame>
  );
}
