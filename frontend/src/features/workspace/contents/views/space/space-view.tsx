import * as React from "react";
import { EntityViewFrame } from "../entity-view-frame";
import { SpaceBoard } from "./space-components/space-board";
import { useSpaceDetail, useGetSpaceItemsQuery, useGetSpaceDetailQuery, useUpdateSpaceFieldMutation, useGetEntityAccessQuery } from "./space-api";
import { Folder, Layout, FileText, GitMerge, Lock, Unlock, MoreVertical } from "lucide-react";
import { CreateStatusForm } from "@/features/workspace/components/forms/workflow-management-form";
import { cn } from "@/lib/utils";
import { PopoverFormWrapper } from "@/components/popover-wrapper";
import { UniversalPicker } from "@/components/universal-picker";
import type { SpaceRecord } from "@/types/projects";
import { useSelector } from "react-redux";
import { entityAccessSelectors, memberSelectors } from "@/store/entityStore";
import { SpaceAccessDialog } from "./space-components/space-access-dialog";

import { SpaceDetail } from "./space-components/space-detail";
import { DynamicIcon } from "@/components/dynamic-icon";

interface SpaceViewProps {
  spaceId: string;
}

export function SpaceView({ spaceId }: Readonly<SpaceViewProps>) {
  const [isWorkflowOpen, setIsWorkflowOpen] = React.useState(false);
  const [activeTab, setActiveTab] = React.useState<"detail" | "items">("items");
  
  // Load space details & items into Redux
  const space = useSpaceDetail(spaceId);
  useGetSpaceDetailQuery(spaceId);
  useGetSpaceItemsQuery(spaceId);

  // Fetch access lists to trigger Redux cache upsertion
  useGetEntityAccessQuery(spaceId);
  const entityAccessList = useSelector(entityAccessSelectors.selectAll).filter(ea => ea.spaceId === spaceId);
  const members = useSelector(memberSelectors.selectEntities);

  const currentAccessMembers = entityAccessList.filter(a => a.haveAccess);

  const [updateSpaceField] = useUpdateSpaceFieldMutation();
  const updateField = (patches: Partial<SpaceRecord>) => {
    updateSpaceField({ spaceId, patches });
  };

  return (
    <EntityViewFrame
      className="bg-card/30"
    >
      <div className="h-full w-full flex flex-col bg-background/25 p-1 gap-1 overflow-hidden relative">
        {/* Ambient background accent glow */}
        <div 
          className="absolute right-12 bottom-12 w-[350px] h-[350px] rounded-full blur-[120px] opacity-[0.05] pointer-events-none transition-all duration-700"
          style={{ backgroundColor: space?.color || "#3b82f6" }}
        />

        {/* Integrated Floating Space Header Bar */}
        <div className="flex items-center justify-between px-2 py-1 rounded-md border border-border/30 bg-card/30 backdrop-blur-md shadow-sm shrink-0">
          <div className="flex items-center gap-1.5">
            <PopoverFormWrapper
              trigger={
                <button className="flex items-center justify-center p-0.5 hover:bg-muted/65 rounded-md transition-all cursor-pointer focus:outline-none border border-border/10 shadow-sm bg-background/80">
                  {space?.icon ? (
                    <DynamicIcon name={space.icon} className="h-3 w-3" color={space.color} />
                  ) : (
                    <Folder className="h-3 w-3" color={space?.color} />
                  )}
                </button>
              }
            >
              <UniversalPicker
                selectedIcon={space?.icon ?? "Folder"}
                selectedColor={space?.color ?? "#3b82f6"}
                onSelect={(icon, color) => updateField({ icon, color })}
              />
            </PopoverFormWrapper>

            <input
              key={spaceId}
              className="h-6 px-1 w-56 text-xs font-bold text-foreground/90 tracking-tight bg-transparent border-none outline-none hover:bg-muted/20 focus:bg-muted/40 transition-all rounded cursor-text"
              defaultValue={space?.name ?? "Space"}
              onPointerDown={(e) => e.stopPropagation()}
              onBlur={(e) => {
                if (e.target.value && e.target.value !== space?.name) {
                  updateField({ name: e.target.value });
                }
              }}
              onKeyDown={(e) => {
                if (e.key === "Enter") e.currentTarget.blur();
              }}
            />
          </div>

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
                const avatarBg = colors[colorIdx];

                return (
                  <div
                    key={access.workspaceMemberId}
                    className={`h-5 w-5 rounded-full ${avatarBg} border border-background flex items-center justify-center text-[8px] font-black text-white shrink-0 shadow-sm hover:translate-y-[-1px] transition-transform cursor-pointer`}
                    title={`${name} (${access.accessLevel})`}
                  >
                    {initials}
                  </div>
                );
              })}

              <SpaceAccessDialog
                spaceId={spaceId}
                trigger={
                  <button className="h-5 w-5 rounded-full bg-muted/65 hover:bg-muted border border-border/20 flex items-center justify-center text-[9px] font-bold text-muted-foreground shrink-0 shadow-sm transition-all cursor-pointer">
                    +
                  </button>
                }
              />
            </div>

            {/* Public/Private Toggle Switcher */}
            {space && (
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
                {space.isPrivate ? (
                  <>
                    <Lock className="h-2 w-2" />
                    <span>Private</span>
                  </>
                ) : (
                  <>
                    <Unlock className="h-2 w-2" />
                    <span>Public</span>
                  </>
                )}
              </button>
            )}

            {/* View Switcher Toggle Buttons */}
            <button
              className="flex items-center h-5 gap-1.5 px-2.5 rounded-md bg-muted/45 text-[10px] text-muted-foreground font-semibold hover:bg-muted hover:text-foreground transition-all cursor-pointer border border-border/10 shadow-sm"
              onClick={() => setIsWorkflowOpen(true)}
            >
              <GitMerge className="h-3 w-3 opacity-80" />
              <span>Workflow</span>
            </button>

            <div className="flex items-center bg-muted/45 border border-border/10 rounded-md p-0.5 shadow-sm">
              <button 
                onClick={() => setActiveTab("items")}
                className={cn(
                  "flex items-center gap-1 h-5 px-2 rounded-sm text-[10px] font-semibold transition-all cursor-pointer",
                  activeTab === "items" 
                    ? "bg-background text-foreground shadow-sm animate-in fade-in duration-200" 
                    : "text-muted-foreground hover:text-foreground"
                )}
              >
                <Layout className="h-2.5 w-2.5" />
                <span>Board</span>
              </button>
              <button 
                onClick={() => setActiveTab("detail")}
                className={cn(
                  "flex items-center gap-1 h-5 px-2 rounded-sm text-[10px] font-semibold transition-all cursor-pointer",
                  activeTab === "detail" 
                    ? "bg-background text-foreground shadow-sm animate-in fade-in duration-200" 
                    : "text-muted-foreground hover:text-foreground"
                )}
              >
                <FileText className="h-2.5 w-2.5" />
                <span>Detail</span>
              </button>
            </div>

            {/* 3-dot vertical menu */}
            <button
              className="h-5 w-5 flex items-center justify-center rounded-md hover:bg-white/[0.06] text-muted-foreground/50 hover:text-foreground transition-all cursor-pointer border border-transparent hover:border-border/20"
              title="More options"
            >
              <MoreVertical className="h-3.5 w-3.5" />
            </button>

          </div>
        </div>

        {/* Content Area */}
        <div className="flex-1 rounded-md border border-border/40 bg-card/30 backdrop-blur-md shadow-sm overflow-hidden flex flex-col relative">
          {activeTab === "items" ? (
            <SpaceBoard spaceId={spaceId} onWorkflowOpen={() => setIsWorkflowOpen(true)} />
          ) : (
            <SpaceDetail spaceId={spaceId} />
          )}
        </div>
      </div>

      {space?.workflowId && (
        <CreateStatusForm
          isOpen={isWorkflowOpen}
          onClose={() => setIsWorkflowOpen(false)}
          workflowId={space.workflowId}
        />
      )}
    </EntityViewFrame>
  );
}
