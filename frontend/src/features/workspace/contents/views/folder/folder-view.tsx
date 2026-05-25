import { EntityViewFrame } from "../entity-view-frame";
import { FolderTaskList } from "./components/folder-task-list";
import { Folder, Trash2, MoreHorizontal, LayoutGrid, GitMerge } from "lucide-react";
import { TaskDetailCanvas } from "../task/components/task-detail-canvas";
import * as React from "react";
import { useParams, Link } from "@tanstack/react-router";
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
import { useGetFolderDetail } from "./folder-api";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { FolderEditorProvider, useFolderEditor } from "./folder-editor-context";
import { FolderTaskBatchBar } from "./components/folder-task-batch-bar";
import { DynamicIcon } from "@/components/dynamic-icon";
import { Priority } from "@/types/priority";
import { PopoverFormWrapper } from "@/components/popover-wrapper";
import { UniversalPicker } from "@/components/universal-picker";
import { Input } from "@/components/ui/input";
import { useFolderRealtime } from "./use-folder-realtime";
import { CreateStatusForm } from "@/features/workspace/components/forms/create-status-form";

export function FolderView() {
  const { folderId, workspaceId } = useParams({ strict: false }) as { folderId: string, workspaceId: string };
  const [checkedTaskIds, setCheckedTaskIds] = React.useState<Set<string>>(new Set());

  useFolderRealtime(folderId);

  const { data: detailData, isLoading } = useGetFolderDetail(folderId);

  const toggleCheck = (id: string, e: React.MouseEvent) => {
    e.stopPropagation();
    if (id === "clear_all") {
      setCheckedTaskIds(new Set());
      return;
    }
    const newSet = new Set(checkedTaskIds);
    if (newSet.has(id)) newSet.delete(id);
    else newSet.add(id);
    setCheckedTaskIds(newSet);
  };

  if (isLoading) {
    return <div className="p-8 text-sm text-muted-foreground">Loading folder...</div>;
  }


  return (
    <FolderEditorProvider folderId={folderId}>
      <InnerFolderView 
        folderId={folderId} 
        workspaceId={workspaceId} 
        checkedTaskIds={checkedTaskIds} 
        setCheckedTaskIds={setCheckedTaskIds} 
        detailData={detailData} 
        toggleCheck={toggleCheck} 
      />
    </FolderEditorProvider>
  );
}

function InnerFolderView({ 
  folderId, 
  workspaceId, 
  checkedTaskIds, 
  setCheckedTaskIds, 
  detailData, 
  toggleCheck 
}: any) {
  const { updateField } = useFolderEditor();
  const folder = detailData?.folder;
  const [isWorkflowOpen, setIsWorkflowOpen] = React.useState(false);

  return (
    <EntityViewFrame
      topHeader={
        <div className="flex items-center justify-between w-full">
          <Breadcrumb className="text-xs">
            <BreadcrumbList className="text-xs sm:gap-1.5">
              <BreadcrumbItem>
                <BreadcrumbLink asChild>
                  <Link to="/workspaces/$workspaceId/spaces/$spaceId" params={{ workspaceId, spaceId: "mock-space" }} className="flex items-center gap-1.5 text-muted-foreground hover:text-foreground">
                    <LayoutGrid className="h-3 w-3" />
                    Engineering
                  </Link>
                </BreadcrumbLink>
              </BreadcrumbItem>
              <BreadcrumbSeparator className="[&>svg]:w-3 [&>svg]:h-3" />
              <BreadcrumbItem>
                <BreadcrumbPage className="font-medium text-foreground flex items-center gap-1.5">
                  {folder?.name || "Folder"}
                </BreadcrumbPage>
              </BreadcrumbItem>
            </BreadcrumbList>
          </Breadcrumb>
          
          <div>
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
        </div>
      }
      subHeader={
        <div className="flex items-center gap-1 w-full h-full">
          <div className="flex items-center gap-2">
            <PopoverFormWrapper
              trigger={
                <button className="flex items-center justify-center p-1 hover:bg-muted rounded-md transition-colors cursor-pointer focus:outline-none">
                  {folder?.icon ? (
                    <DynamicIcon name={folder.icon} className="h-4 w-4" color={folder?.color} />
                  ) : (
                    <Folder className="h-4 w-4" color={folder?.color} />
                  )}
                </button>
              }
            >
              <UniversalPicker
                selectedIcon={folder?.icon || "Folder"}
                selectedColor={folder?.color || "#6366f1"}
                onSelect={(icon, color) => updateField({ icon, color })}
              />
            </PopoverFormWrapper>
            <input
              className="h-6 px-1  -ml-1 w-63 text-sm font-bold text-foreground tracking-tight bg-transparent border-none outline-none hover:bg-muted/30 focus:bg-muted/50 transition-colors rounded-sm cursor-text"
              defaultValue={folder?.name || "Folder"}
              onBlur={(e) => {
                if (e.target.value && e.target.value !== folder?.name) {
                  updateField({ name: e.target.value });
                }
              }}
              onKeyDown={(e) => {
                if (e.key === "Enter") {
                  e.currentTarget.blur();
                }
              }}
            />
          </div>
          
          <div className="flex items-center gap-3">
            <DropdownMenu>
              <DropdownMenuTrigger className="focus:outline-none">
                <PriorityBadge priority={folder?.priority} />
              </DropdownMenuTrigger>
              <DropdownMenuContent>
                {Object.values(Priority).map(p => (
                  <DropdownMenuItem key={p} onClick={() => updateField({ priority: p })}>
                    <PriorityBadge priority={p} />
                  </DropdownMenuItem>
                ))}
              </DropdownMenuContent>
            </DropdownMenu>
            
            <PopoverFormWrapper
              trigger={
                <button className="focus:outline-none flex items-center">
                  <DateBadge startDate={folder?.startDate} dueDate={folder?.dueDate} />
                </button>
              }
              className="w-auto p-3"
            >
              <div className="flex flex-col gap-3">
                <div className="flex flex-col gap-1.5">
                  <label className="text-[10px] font-bold uppercase text-muted-foreground">Start Date</label>
                  <Input 
                    type="date" 
                    className="h-8 text-xs"
                    defaultValue={folder?.startDate?.split('T')[0] || ""}
                    onBlur={(e) => updateField({ startDate: e.target.value ? new Date(e.target.value).toISOString() : undefined })}
                  />
                </div>
                <div className="flex flex-col gap-1.5">
                  <label className="text-[10px] font-bold uppercase text-muted-foreground">Due Date</label>
                  <Input 
                    type="date" 
                    className="h-8 text-xs"
                    defaultValue={folder?.dueDate?.split('T')[0] || ""}
                    onBlur={(e) => updateField({ dueDate: e.target.value ? new Date(e.target.value).toISOString() : undefined })}
                  />
                </div>
              </div>
            </PopoverFormWrapper>
            
            <button 
              className="flex items-center h-5 gap-1.5 px-2 rounded-sm bg-muted/50 text-[10px] text-muted-foreground font-semibold hover:bg-muted/80 transition-colors cursor-pointer"
              onClick={() => setIsWorkflowOpen(true)}
            >
              <GitMerge className="h-3 w-3 opacity-70" />
              <span>Workflow</span>
            </button>
          </div>
          
          {folder?.createdAt && (
            <div className="ml-auto flex items-center text-[11px] text-muted-foreground">
              Created {new Date(folder.createdAt).toLocaleDateString()}
            </div>
          )}
        </div>
      }
    >
      <div className="h-full w-full flex relative">
        <div className="w-[300px] border-r border-border shrink-0 flex flex-col bg-background/50">
          <FolderTaskList 
            checkedTaskIds={checkedTaskIds}
            onToggleCheck={toggleCheck}
            statuses={detailData?.statuses}
          />
        </div>

        <div className="flex-1 h-full overflow-hidden bg-background">
          <TaskDetailCanvas taskId="1" />
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
          statuses={detailData?.statuses || []}
        />
      )}
    </EntityViewFrame>
  );
}
