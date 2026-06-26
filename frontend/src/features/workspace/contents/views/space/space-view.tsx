import * as React from "react";
import { useSpaceAccess } from "@/features/workspace/context/use-space-access";
import { EntityViewFrame } from "../entity-view-frame";
import { SpaceBoard } from "./space-components/space-board";
import { useSpaceDetail, useGetSpaceDetailQuery, useUpdateSpaceFieldMutation, useGetEntityAccessQuery } from "./space-api";
import { Layout, FileText, GitMerge, Lock, Unlock, MoreVertical, Trash2 } from "lucide-react";
import { FavoriteButton } from "@/components/favorite-button";
import { DynamicIcon } from "@/components/dynamic-icon";
import { EntityLayerType } from "@/types/entity-layer-type";
import { CreateStatusForm } from "@/features/workspace/components/forms/workflow-management-form";
import { cn } from "@/lib/utils";

import type { SpaceRecord } from "@/types/projects";
import { useSelector } from "react-redux";
import { entityAccessSelectors, memberSelectors } from "@/store/entityStore";
import { SpaceAccessDialog } from "./space-components/space-access-dialog";
import { useNavigate } from "@tanstack/react-router";
import { useDeleteSpaceMutation } from "../../hierarchy/hierarchy-api";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { SpaceDetail } from "./space-components/space-detail";
import { DeleteConfirmationDialog } from "../../hierarchy/hierarchy-components/context-menus/shared";


interface SpaceViewProps {
  spaceId: string;
}

export function SpaceView({ spaceId }: Readonly<SpaceViewProps>) {
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
  const {canManage } = useSpaceAccess(spaceId);
  const navigate = useNavigate();
  const [deleteSpace] = useDeleteSpaceMutation();
  
  // Load space details & items into Redux
  const space = useSpaceDetail(spaceId);
  useGetSpaceDetailQuery(spaceId);
  // Fetch access lists to trigger Redux cache upsertion
  useGetEntityAccessQuery(spaceId);
  const entityAccessList = useSelector(entityAccessSelectors.selectAll).filter(ea => ea.spaceId === spaceId);
  const members = useSelector(memberSelectors.selectEntities);

  const currentAccessMembers = entityAccessList.filter(a => a.haveAccess);

  const [updateSpaceField] = useUpdateSpaceFieldMutation();
  const updateField = (patches: Partial<SpaceRecord>) => {
    updateSpaceField({ spaceId, patches });
  };

  const handleDelete = async () => {
    await deleteSpace({ workspaceId: space?.workspaceId?.toString() ?? "", spaceId });
    navigate({ to: "/workspaces/$workspaceId", params: { workspaceId: space?.workspaceId?.toString() ?? "" } });
  };

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
      <div className="h-full w-full flex flex-col bg-transparent p-1 gap-1 overflow-hidden relative">
        {/* Controls toolbar */}
        <div className="flex items-center justify-between px-2 py-1 rounded-md border border-border bg-card shadow-sm shrink-0">
          {/* Public/Private toggle — admin only, sits on the left */}
          {space && canManage ? (
            <button
              onClick={() => updateField({ isPrivate: !space.isPrivate })}
              className={cn(
                "flex items-center gap-1 h-5 px-1.5 rounded-md border text-[9px] font-bold cursor-pointer transition-all select-none",
                space.isPrivate
                  ? "bg-rose-500/10 border-rose-500/30 text-rose-400 hover:bg-rose-500/20"
                  : "bg-emerald-500/10 border-emerald-500/30 text-emerald-400 hover:bg-emerald-500/20"
              )}
              title={space.isPrivate ? "Click to make Public" : "Click to make Private"}
            >
              {space.isPrivate ? <><Lock className="h-2 w-2" /><span>Private</span></> : <><Unlock className="h-2 w-2" /><span>Public</span></>}
            </button>
          ) : <div />}

          <div className="flex items-center gap-2.5">
            {/* Space Members Avatar Pile */}
            <div className="flex items-center -space-x-1.5 mr-0.5 select-none">
              {currentAccessMembers.slice(0, 4).map((access) => {
                const profile = members[access.workspaceMemberId];
                if (!profile) return null;
                const name = profile.name || "User";
                const initials = name.split(" ").map((w: string) => w[0]).slice(0, 2).join("").toUpperCase();
                const colors = ["bg-cyan-500", "bg-purple-500", "bg-indigo-500", "bg-teal-500", "bg-emerald-500", "bg-amber-500"];
                const colorIdx = (initials.charCodeAt(0) + (initials.charCodeAt(1) || 0)) % colors.length;
                return (
                  <div
                    key={access.workspaceMemberId}
                    className={`h-5 w-5 rounded-full ${colors[colorIdx]} border border-background flex items-center justify-center text-[8px] font-black text-white shrink-0 shadow-sm hover:-translate-y-px transition-transform cursor-pointer`}
                    title={`${name} (${access.accessLevel})`}
                  >
                    {initials}
                  </div>
                );
              })}
              {canManage && (
                <SpaceAccessDialog
                  spaceId={spaceId}
                  trigger={
                    <button className="h-5 w-5 rounded-full bg-muted/65 hover:bg-muted border border-border/20 flex items-center justify-center text-[9px] font-bold text-muted-foreground shrink-0 shadow-sm transition-all cursor-pointer">
                      +
                    </button>
                  }
                />
              )}
            </div>

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
        <div className="flex-1 rounded-md border border-border bg-black/5 dark:bg-black/20 shadow-sm overflow-hidden flex flex-col relative">
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
}
