import { EntityViewFrame } from "../entity-view-frame";
import { FolderTaskList } from "./components/folder-task-list";
import { Trash2, MoreVertical, Maximize2 } from "lucide-react";
import { FavoriteButton } from "@/components/favorite-button";
import { EntityLayerType } from "@/types/entity-layer-type";
import { TaskDetailCanvas } from "../task/components/task-detail-canvas";
import * as React from "react";
import { observer } from "mobx-react-lite";
import { useParams, Link, useNavigate } from "@tanstack/react-router";
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
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { DateSelect } from "@/components/date-select";
import { FolderTaskBatchBar } from "./components/folder-task-batch-bar";
import { UniversalPicker } from "@/components/universal-picker";
import { cn } from "@/lib/utils";
import { DeleteConfirmationDialog } from "../../hierarchy/hierarchy-components/context-menus/shared";
import { useStore } from "@/stores/root.store";
import { useSyncEngine, useSyncReady } from "@/sync/sync-provider";
import { useDebouncedFlush } from "@/sync/use-debounced-flush";
import { FolderMutations } from "@/mutations/folder.mutations";
import { TaskMutations } from "@/mutations/task.mutations";

interface FolderViewProps {
  folderId: string;
}

export const FolderView = observer(function FolderView({ folderId }: Readonly<FolderViewProps>) {
  const { workspaceId } = useParams({ strict: false }) as {
    workspaceId: string;
  };

  const { isAdmin } = useWorkspaceRole();
  const rootStore = useStore();
  const syncEngine = useSyncEngine();
  const { ready, error } = useSyncReady();
  const folderMutations = React.useMemo(() => new FolderMutations(rootStore, syncEngine), [rootStore, syncEngine]);
  const taskMutations = React.useMemo(() => new TaskMutations(rootStore, syncEngine), [rootStore, syncEngine]);
  const { scheduleFlush } = useDebouncedFlush(syncEngine);

  const folder = rootStore.folderStore.getById(folderId);
  const canManageSpace = isAdmin;
  const [checkedTaskIds, setCheckedTaskIds] = React.useState<Set<string>>(new Set());
  const [isDeleteOpen, setIsDeleteOpen] = React.useState(false);
  const [selectedTaskIdState, setSelectedTaskIdState] = React.useState<string | undefined>(undefined);

  // Plain read, not useMemo — this component is a mobx-react-lite `observer`, which tracks
  // observable reads made directly during render. rootStore.taskStore is the same object
  // instance for the life of the workspace (MobX mutates the map in place), so a useMemo keyed
  // on that reference would never recompute when tasks inside it change — the exact bug that
  // made this list go stale (edits from TaskDetailCanvas, status colors, reorder) until a
  // refresh forced a full remount.
  const tasks = rootStore.taskStore.getByFolder(folderId).filter((t) => !t.parentTaskId);

  const parentSpace = folder?.spaceId ? rootStore.spaceStore.getById(folder.spaceId) : undefined;

  const selectedTaskId = React.useMemo(() => {
    if (tasks.length === 0) return undefined;
    const exists = tasks.some(t => t.id === selectedTaskIdState);
    if (selectedTaskIdState && exists) return selectedTaskIdState;

    const lastVisited = localStorage.getItem(`lastVisitedTask_${folderId}`);
    const lastVisitedExists = lastVisited && tasks.some(t => t.id === lastVisited);
    return lastVisitedExists ? lastVisited : tasks[0].id;
  }, [tasks, selectedTaskIdState, folderId]);

  const setSelectedTaskId = setSelectedTaskIdState;

  React.useEffect(() => {
    if (selectedTaskId) {
      localStorage.setItem(`lastVisitedTask_${folderId}`, selectedTaskId);
    }
  }, [selectedTaskId, folderId]);

  // Same reason as `tasks` above — plain read, not useMemo.
  const taskStatuses = folder?.spaceId ? rootStore.statusStore.getBySpace(folder.spaceId) : [];

  // Instant local write (store/IndexedDB/queue) + debounced network flush — same pattern as
  // TaskDetailCanvas's useDebouncedTaskUpdate.
  const updateField = (patches: Parameters<FolderMutations["updateLocal"]>[1]) => {
    folderMutations.updateLocal(folderId, patches).catch((err) => console.error("Failed to apply local folder update", err));
    scheduleFlush();
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

  const navigate = useNavigate();

  if (error) {
    return (
      <div className="flex flex-col items-center justify-center h-full text-destructive/80 space-y-2 p-8">
        <DynamicIcon name="AlertTriangle" size={32} />
        <span className="text-sm font-medium">Failed to load folder</span>
      </div>
    );
  }

  if (ready && !folder) {
    return (
      <div className="flex flex-col items-center justify-center h-full text-destructive/80 space-y-2 p-8">
        <DynamicIcon name="AlertTriangle" size={32} />
        <span className="text-sm font-medium">Folder Not Found</span>
        <span className="text-xs text-muted-foreground">The folder may have been deleted by another user.</span>
      </div>
    );
  }

  if (!ready || !folder) {
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
                          size={15}
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
                    size={15}
                    color={folder?.color || "#6366f1"}
                    className="stroke-[2.5] shrink-0"
                  />
                  {folder?.name ?? "Folder"}
                  {folder && (
                    <FavoriteButton
                      entityId={folder.id}
                      entityLayerType={EntityLayerType.ProjectFolder}
                      iconSize={13}
                      className="opacity-100"
                    />
                  )}
                </BreadcrumbPage>
              </BreadcrumbItem>
            </BreadcrumbList>
          </Breadcrumb>

          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="h-7 w-7 text-muted-foreground">
                <MoreVertical className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              {(isAdmin || canManageSpace) && (
                <DropdownMenuItem
                  onClick={() => setIsDeleteOpen(true)}
                  className="text-destructive focus:text-destructive focus:bg-destructive/10 cursor-pointer"
                >
                  <Trash2 className="h-4 w-4 mr-2" />
                  Delete Folder
                </DropdownMenuItem>
              )}
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      }
    >
      <div className="h-full w-full flex flex-col bg-transparent p-1 gap-1 overflow-hidden relative">
        {/* Integrated Floating Folder Header Bar */}
        <div className="flex items-center justify-between px-1.5 py-1 rounded-md border border-border bg-card shadow-sm shrink-0">
          <div className="flex items-center gap-1.5">
            <UniversalPicker
              icon={folder?.icon ?? "Folder"}
              color={folder?.color ?? "#6366f1"}
              onSelect={(icon, color) => updateField({ icon, color })}
              size="md"
            />

            <input
              key={folderId}
              className="h-6 px-1 w-56 text-xs font-bold text-foreground/90 tracking-tight bg-transparent border-none outline-none hover:bg-muted/20 focus:bg-muted/40 transition-all rounded cursor-text"
              defaultValue={folder?.name ?? "Folder"}
              onPointerDown={(e) => e.stopPropagation()}
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
            <DateSelect
              startDate={folder?.startDate}
              dueDate={folder?.dueDate}
              onStartDateChange={(date) => updateField({ startDate: date ? date.toISOString() : null })}
              onDueDateChange={(date) => updateField({ dueDate: date ? date.toISOString() : null })}
              align="end"
              size="sm"
              triggerClassName="h-5 px-2 text-[10px] font-semibold rounded-md border border-border/10 bg-muted/40 hover:bg-muted/75 hover:text-foreground text-muted-foreground transition-all cursor-pointer shadow-sm"
            />

          </div>
        </div>

        {/* Floating Content Area */}
        <div className="flex-1 flex gap-1 overflow-hidden relative">
          {/* Left Card: Folder Task List Column */}
          <div className={cn(
            "rounded-md border border-border bg-card shadow-sm overflow-hidden flex flex-col shrink-0 transition-all duration-300",
            selectedTaskId ? "w-70" : "flex-1 w-full"
          )}>
            <FolderTaskList
              folderId={folderId}
              tasks={tasks}
              taskStatuses={taskStatuses}
              spaceId={folder.spaceId}
              taskMutations={taskMutations}
              scheduleFlush={scheduleFlush}
              checkedTaskIds={checkedTaskIds}
              onToggleCheck={toggleCheck}
              selectedTaskId={selectedTaskId}
              onSelectTask={setSelectedTaskId}
            />
          </div>

          {/* Right Card: Task Detail Canvas */}
          {selectedTaskId && (
            <div className="flex-1 rounded-md border border-border bg-card shadow-sm overflow-hidden flex flex-col relative group">
              <div className="absolute top-1.5 right-1.5 z-10 opacity-0 group-hover:opacity-100 transition-opacity">
                <Button
                  variant="ghost"
                  size="icon"
                  className="h-7 w-7  text-muted-foreground hover:text-foreground backdrop-blur-md"
                  title="Open full view"
                  onClick={() => {
                    navigate({
                      to: `/workspaces/$workspaceId/tasks/$taskId`,
                      params: { workspaceId, taskId: selectedTaskId }
                    });
                  }}
                >
                  <Maximize2 className="h-3.5 w-3.5" />
                </Button>
              </div>
              <TaskDetailCanvas taskId={selectedTaskId} />
            </div>
          )}
        </div>
      </div>

      <DeleteConfirmationDialog
        open={isDeleteOpen}
        onOpenChange={setIsDeleteOpen}
        title="Delete Folder"
        description={`Are you sure you want to delete "${folder?.name}"? This will delete all tasks inside it and cannot be undone.`}
        onConfirm={() => {
          folderMutations.delete(folderId).catch((err) => console.error("Failed to delete folder", err));
          navigate({ to: "/workspaces/$workspaceId", params: { workspaceId } });
        }}
      />

      {checkedTaskIds.size > 0 && (
        <FolderTaskBatchBar
          checkedTaskIds={checkedTaskIds}
          onClear={() => setCheckedTaskIds(new Set())}
          statuses={taskStatuses}
          taskMutations={taskMutations}
        />
      )}
    </EntityViewFrame>
  );
});
