import { useSpaceDetail, useGetEntityAccessQuery, useUpdateEntityAccessMutation, useSpaceStatuses, useSpaceBoardItems } from "../space-api";
import type { AccessLevel } from "@/types/access-level";
import { Shield } from "lucide-react";
import  { useState, useMemo } from "react";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import type { EntityAccessRecord } from "@/types/workspace";
import { StatusBadge } from "@/components/status-badge";
import { useSelector } from "react-redux";
import { entityAccessSelectors, memberSelectors } from "@/store/entityStore";
import { SpaceAccessDialog } from "./space-access-dialog";
import { CreateStatusForm } from "@/features/workspace/components/forms/create-status-form";
import { SpaceDocumentsPanel } from "./space-documents-panel";

interface SpaceDetailProps {
  spaceId: string;
}

interface SpaceAccessMemberRowProps {
  access: EntityAccessRecord;
  spaceCreatorId?: string;
  onAccessChange: (memberId: string, newLevel: AccessLevel) => void;
}

export function SpaceAccessMemberRow({ access, spaceCreatorId, onAccessChange }: Readonly<SpaceAccessMemberRowProps>) {
  const members = useSelector(memberSelectors.selectEntities);
  const profile = members[access.workspaceMemberId];
  if (!profile) return null;

  const name = profile.name || "Unknown User";
  const initials = name.split(" ").map((w: string) => w[0]).slice(0, 2).join("").toUpperCase();
  const isCreator = !!(spaceCreatorId && access.workspaceMemberId === spaceCreatorId);

  return (
    <div className="flex items-center gap-1.5 p-1 rounded-sm hover:bg-white/[0.02] transition-colors cursor-pointer group/member">
      <Avatar className="h-4 w-4 shrink-0 text-[8px] font-black">
        <AvatarImage src={profile.avatarUrl} alt={name} />
        <AvatarFallback className="bg-primary/20 text-white border border-border/20 leading-none flex items-center justify-center">
          {initials}
        </AvatarFallback>
      </Avatar>
      <div className="flex flex-col">
        <span className="font-bold text-[10px] text-foreground/85 line-clamp-1 leading-none flex items-center gap-1">
          {name}
          {isCreator && (
            <span className="text-[6px] bg-primary/25 text-primary px-0.5 rounded-sm uppercase tracking-wider font-extrabold scale-90 origin-left">
              Owner
            </span>
          )}
        </span>
        <span className="text-[7.5px] text-muted-foreground/45 flex items-center gap-0.5 font-medium uppercase font-mono mt-0.5">
          <Shield className="h-2 w-2 shrink-0 opacity-55" />
          {profile.role || "Member"}
        </span>
      </div>
      
      <select
        value={isCreator ? "Manager" : access.accessLevel}
        onChange={(e) => onAccessChange(access.workspaceMemberId, e.target.value as AccessLevel)}
        disabled={isCreator}
        className="ml-auto bg-background border border-border/25 rounded px-1 py-0.5 text-[9px] font-bold text-foreground/80 cursor-pointer disabled:cursor-not-allowed disabled:opacity-50 outline-none focus:border-primary/50 leading-none"
        title={isCreator ? "Owner has full management access" : "Change access privilege"}
      >
        {isCreator ? (
          <option value="Manager">Owner</option>
        ) : (
          <>
            <option value="Viewer">Viewer</option>
            <option value="Editor">Editor</option>
            <option value="Manager">Manager</option>
            <option value="None">Remove</option>
          </>
        )}
      </select>
    </div>
  );
}

export function SpaceDetail({ spaceId }: SpaceDetailProps) {
  const space = useSpaceDetail(spaceId);

  // Collapse toggle state for sidebar lists
  const [isMembersCollapsed, setIsMembersCollapsed] = useState(false);
  const [isWorkflowCollapsed, setIsWorkflowCollapsed] = useState(false);
  const [isWorkflowOpen, setIsWorkflowOpen] = useState(false);

  // Fetch access lists to trigger the onQueryStarted handler which populates Redux
  useGetEntityAccessQuery(spaceId);
  const entityAccessList = useSelector(entityAccessSelectors.selectAll).filter(ea => ea.spaceId === spaceId);
  const [updateEntityAccess] = useUpdateEntityAccessMutation();

  // Retrieve dynamic space-scoped statuses & items
  const spaceStatuses = useSpaceStatuses(spaceId);
  const spaceItems = useSpaceBoardItems(spaceId);

  // Compute live item counts per status ID
  const countPerStatus = useMemo(() => {
    const counts: Record<string, number> = {};
    spaceItems.forEach((item) => {
      if (item.statusId) {
        counts[item.statusId] = (counts[item.statusId] || 0) + 1;
      }
    });
    return counts;
  }, [spaceItems]);

  // Filter only members who explicitly have access
  const currentAccessMembers = entityAccessList.filter(a => a.haveAccess);

  if (!space) return null;

  // Handle changing member access level on click/select
  const handleAccessChange = async (memberId: string, newLevel: AccessLevel, isCreate: boolean) => {
    const action = isCreate ? "Create" : (newLevel === "None" ? "Delete" : "Update");
    const access = entityAccessList.find((a) => a.workspaceMemberId === memberId);

    try {
      await updateEntityAccess({
        spaceId,
        rows: [{
          id: access?.id,
          memberId,
          accessLevel: newLevel,
          action
        }]
      }).unwrap();
    } catch (e) {
      console.error("Failed to update space access level:", e);
    }
  };

  return (
    <div className="flex h-full w-full bg-transparent overflow-hidden text-foreground">
      
      {/* 1. Left Column: Main Details Canvas */}
      <div className="flex-1 overflow-y-auto p-2 space-y-3 relative [&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-thumb]:bg-white/[0.05] [&::-webkit-scrollbar-thumb]:rounded-full hover:[&::-webkit-scrollbar-thumb]:bg-white/[0.15]">
        {/* Real Dynamic Space Documents Panel */}
        <SpaceDocumentsPanel spaceId={spaceId} />
      </div>

      {/* 2. Right Column: Fixed Floating Sidebar */}
      <div className="w-[280px] shrink-0 flex flex-col gap-1.5 p-1.5 overflow-y-auto bg-transparent select-none [&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-thumb]:bg-white/[0.05] [&::-webkit-scrollbar-thumb]:rounded-full">
        
        {/* Members Block: Powered by Entity Access */}
        <div className="p-1.5 rounded-md border border-border/20 bg-card hover:bg-card/85 transition-all duration-200 space-y-1.5 shadow-sm">
          <div className="flex items-center justify-between">
            <span
              onClick={() => setIsMembersCollapsed(!isMembersCollapsed)}
              className="text-[11px] font-bold text-foreground/80 flex items-center gap-1 cursor-pointer hover:text-foreground select-none"
            >
              Members <span className={`text-[8px] text-muted-foreground/60 transition-transform duration-200 inline-block ${isMembersCollapsed ? "" : "rotate-90"}`}>▶</span>
            </span>

            {/* Premium Table dialog form containing all members in workspace */}
            <SpaceAccessDialog
              spaceId={spaceId}
              trigger={
                <button className="text-muted-foreground/60 hover:text-foreground text-[10px] font-bold cursor-pointer select-none px-1.5 py-0.5 rounded hover:bg-white/[0.03]">
                  +
                </button>
              }
            />
          </div>
          
            <div className="space-y-1 text-xs transition-all duration-300">
              {currentAccessMembers.length === 0 ? (
                <div className="text-[9px] text-muted-foreground/45 italic py-0.5 px-1">No explicit members assigned</div>
              ) : (
                currentAccessMembers.map((access) => (
                  <SpaceAccessMemberRow
                    key={access.workspaceMemberId}
                    access={access}
                    spaceCreatorId={space.creatorId}
                    onAccessChange={(memberId, newLevel) => handleAccessChange(memberId, newLevel, false)}
                  />
                ))
              )}
            </div>
        </div>

        {/* Workflow Block */}
        <div className="p-1.5 rounded-md border border-border/20 bg-card hover:bg-card/85 transition-all duration-200 space-y-1.5 shadow-sm">
          <div className="flex items-center justify-between">
            <span
              onClick={() => setIsWorkflowCollapsed(!isWorkflowCollapsed)}
              className="text-[11px] font-bold text-foreground/80 flex items-center gap-1 cursor-pointer hover:text-foreground select-none"
            >
              Workflow <span className={`text-[8px] text-muted-foreground/60 transition-transform duration-200 inline-block ${isWorkflowCollapsed ? "" : "rotate-90"}`}>▶</span>
            </span>
            {space.workflowId && (
              <button
                onClick={() => setIsWorkflowOpen(true)}
                className="text-muted-foreground/60 hover:text-foreground text-[10px] font-bold cursor-pointer select-none px-1.5 py-0.5 rounded hover:bg-white/[0.03]"
              >
                +
              </button>
            )}
          </div>
          
          {!isWorkflowCollapsed && (
            <div className="space-y-1 text-xs">
              {spaceStatuses.length === 0 ? (
                <div className="text-[9px] text-muted-foreground/45 italic py-0.5 px-1">No statuses defined</div>
              ) : (
                spaceStatuses.map((status) => {
                  const count = countPerStatus[status.id] || 0;
                  return (
                    <div key={status.id} className="flex items-center justify-between p-1 rounded-sm hover:bg-white/[0.02] transition-colors cursor-pointer">
                      <StatusBadge status={status} />
                      <span className="text-[8px] font-mono text-muted-foreground/40 font-bold">
                        {count} {count === 1 ? "Item" : "Items"}
                      </span>
                    </div>
                  );
                })
              )}
            </div>
          )}
        </div>

      </div>

      {space.workflowId && (
        <CreateStatusForm
          isOpen={isWorkflowOpen}
          onClose={() => setIsWorkflowOpen(false)}
          workflowId={space.workflowId}
          currentStatuses={spaceStatuses}
        />
      )}

    </div>
  );
}
