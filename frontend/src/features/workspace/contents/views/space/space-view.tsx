import * as React from "react";
import { observer } from "mobx-react-lite";
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";
import { EntityViewFrame } from "../entity-view-frame";
import { SpaceBoard } from "./space-components/space-board";
import { Layout, FileText, GitMerge, MoreVertical, Trash2 } from "lucide-react";
import { FavoriteButton } from "@/components/favorite-button";
import { DynamicIcon } from "@/components/dynamic-icon";
import { EntityLayerType } from "@/types/entity-layer-type";
import { CreateStatusForm } from "@/features/workspace/components/forms/workflow-management-form";
import { cn } from "@/lib/utils";

import { useNavigate } from "@tanstack/react-router";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { SpaceDetail } from "./space-components/space-detail";
import { DeleteConfirmationDialog } from "../../hierarchy/hierarchy-components/context-menus/shared";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useSyncEngine, useSyncReady } from "@/sync/sync-provider";
import { SpaceMutations } from "@/mutations/space.mutations";


interface SpaceViewProps {
  spaceId: string;
}

export const SpaceView = observer(function SpaceView({ spaceId }: Readonly<SpaceViewProps>) {
  const [isWorkflowOpen, setIsWorkflowOpen] = React.useState(false);
  const [isDeleteOpen, setIsDeleteOpen] = React.useState(false);
  const [activeTab, setActiveTab] = React.useState<"detail" | "items">(() => {
    const saved = localStorage.getItem(`global-space-tab`);
    return saved === "items" ? "items" : "detail";
  });
  const handleTabChange = (tab: "detail" | "items") => {
    setActiveTab(tab);
    localStorage.setItem(`global-space-tab`, tab);
  };
  const { isAdmin: canManage } = useWorkspaceRole();
  const navigate = useNavigate();
  const rootStore = useWorkspaceRootStore();
  const syncEngine = useSyncEngine();
  const { ready, error } = useSyncReady();
  const spaceMutations = React.useMemo(() => new SpaceMutations(rootStore, syncEngine), [rootStore, syncEngine]);

  const space = rootStore.spaceStore.getById(spaceId);

  const handleDelete = async () => {
    const workspaceId = space?.workspaceId ?? "";
    try {
      await spaceMutations.delete(spaceId);
      navigate({ to: "/workspaces/$workspaceId", params: { workspaceId } });
    } catch (err) {
      console.error("Failed to delete space", err);
    }
  };

  if (!space) {
    if (error) {
      return (
        <div className="flex flex-col items-center justify-center h-full text-destructive/80 space-y-2 p-8">
          <DynamicIcon name="AlertTriangle" size={32} />
          <span className="text-sm font-medium">Failed to load space</span>
        </div>
      );
    }

    if (ready) {
      return (
        <div className="flex flex-col items-center justify-center h-full text-destructive/80 space-y-2 p-8">
          <DynamicIcon name="AlertTriangle" size={32} />
          <span className="text-sm font-medium">Space Not Found</span>
          <span className="text-xs text-muted-foreground">The space may have been deleted by another user.</span>
        </div>
      );
    }

    return <div className="p-8 text-sm text-muted-foreground animate-pulse">Loading space...</div>;
  }

  return (
    <EntityViewFrame
      topHeader={
        <div className="flex items-center justify-between w-full">
          <div className="flex items-center gap-1.5 text-xs min-w-0">
            <DynamicIcon
              name={space?.icon ?? "LayoutGrid"}
              size={14}
              color={space?.color ?? "#3b82f6"}
              className="shrink-0"
            />
            <span className="font-semibold text-foreground/80 truncate">
              {space?.name ?? "Space"}
            </span>
            {space && (
              <FavoriteButton
                entityId={spaceId}
                entityLayerType={EntityLayerType.ProjectSpace}
                iconSize={13}
                className="opacity-100 shrink-0"
              />
            )}
          </div>

          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="h-7 w-7 text-muted-foreground">
                <MoreVertical className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              {canManage && (
                <DropdownMenuItem
                  onClick={() => setIsDeleteOpen(true)}
                  className="text-destructive focus:text-destructive focus:bg-destructive/10 cursor-pointer"
                >
                  <Trash2 className="h-4 w-4 mr-2" />
                  Delete Space
                </DropdownMenuItem>
              )}
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      }
    >
      <div className="h-full w-full flex flex-col bg-card p-1 gap-1 overflow-hidden relative">
        {/* Controls toolbar */}
        <div className="flex items-center justify-end px-2 py-1 rounded-md border border-border bg-card shadow-sm shrink-0">
          <div className="flex items-center gap-2.5">
            {/* Workflow — manager+ only */}
            {canManage && (
              <button
                className="flex items-center h-5 gap-1.5 px-2.5 rounded-md bg-muted/45 text-[10px] text-muted-foreground font-semibold hover:bg-muted hover:text-foreground transition-all cursor-pointer border border-border/10 shadow-sm"
                onClick={() => setIsWorkflowOpen(true)}
              >
                <GitMerge className="h-3 w-3 opacity-80" />
                <span>Workflow</span>
              </button>
            )}

            {/* View tabs */}
            <div className="flex items-center bg-secondary/50 border border-transparent rounded-md p-0.5 shadow-inner">
              <button
                onClick={() => handleTabChange("detail")}
                className={cn(
                  "flex items-center gap-1 h-5 px-2 rounded-md text-[10px] font-semibold transition-all cursor-pointer",
                  activeTab === "detail" ? "bg-background text-foreground shadow-sm animate-in fade-in duration-200" : "text-muted-foreground hover:text-foreground"
                )}
              >
                <FileText className="h-2.5 w-2.5" />
                <span>Board</span>
              </button>
              <button
                onClick={() => handleTabChange("items")}
                className={cn(
                  "flex items-center gap-1 h-5 px-2 rounded-md text-[10px] font-semibold transition-all cursor-pointer",
                  activeTab === "items" ? "bg-background text-foreground shadow-sm animate-in fade-in duration-200" : "text-muted-foreground hover:text-foreground"
                )}
              >
                <Layout className="h-2.5 w-2.5" />
                <span>Tasks</span>
              </button>
            </div>
          </div>
        </div>

        {/* Content area */}
        <div className="flex-1 rounded-md border border-border bg-card shadow-sm overflow-hidden flex flex-col relative">
          {activeTab === "items" ? (
            <SpaceBoard spaceId={spaceId} onWorkflowOpen={canManage ? () => setIsWorkflowOpen(true) : undefined} />
          ) : (
            <SpaceDetail spaceId={spaceId} />
          )}
        </div>
      </div>

      <CreateStatusForm
        isOpen={isWorkflowOpen}
        onClose={() => setIsWorkflowOpen(false)}
        spaceId={space?.id}
      />

      <DeleteConfirmationDialog
        open={isDeleteOpen}
        onOpenChange={setIsDeleteOpen}
        title="Delete Space"
        description={`Are you sure you want to delete "${space?.name}"? This will delete all folders and tasks inside it and cannot be undone.`}
        onConfirm={handleDelete}
      />
    </EntityViewFrame>
  );
});
