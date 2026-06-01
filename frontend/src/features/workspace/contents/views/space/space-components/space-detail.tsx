import { useSpaceDetail, useGetEntityAccessQuery, useUpdateEntityAccessMutation, useSpaceStatuses, useSpaceBoardItems } from "../space-api";
import { Shield } from "lucide-react";
import  { useState, useMemo } from "react";
import { StatusBadge } from "@/components/status-badge";
import { useWorkspace } from "@/features/workspace/context/workspace-provider";
import { useSelector } from "react-redux";
import { entityAccessSelectors } from "@/store/entityStore";
import { SpaceAccessDialog } from "./space-access-dialog";
import { CreateStatusForm } from "@/features/workspace/components/forms/create-status-form";
import { SpaceDocumentsPanel } from "./space-documents-panel";

interface SpaceDetailProps {
  spaceId: string;
}

export function SpaceDetail({ spaceId }: SpaceDetailProps) {
  const space = useSpaceDetail(spaceId);
  const { registry } = useWorkspace();

  // Collapse toggle state for sidebar lists
  const [isMembersCollapsed, setIsMembersCollapsed] = useState(false);
  const [isWorkflowCollapsed, setIsWorkflowCollapsed] = useState(false);
  const [isWorkflowOpen, setIsWorkflowOpen] = useState(false);

  // Fetch access lists to trigger the onQueryStarted handler which populates Redux
  useGetEntityAccessQuery(spaceId);
  const entityAccessList = useSelector(entityAccessSelectors.selectAll);
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

  // Handle changing member access level on click
  const handleAccessChange = async (memberId: string, currentLevel: string, isCreate: boolean) => {
    const nextLevels: Record<string, "None" | "Viewer" | "Editor" | "Manager"> = {
      None: "Viewer",
      Viewer: "Editor",
      Editor: "Manager",
      Manager: "None"
    };

    const nextLevel = nextLevels[currentLevel] || "Viewer";
    const action = isCreate ? "Create" : (nextLevel === "None" ? "Delete" : "Update");

    try {
      await updateEntityAccess({
        spaceId,
        rows: [{
          memberId,
          accessLevel: nextLevel,
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
          
          {!isMembersCollapsed && (
            <div className="space-y-1 text-xs transition-all duration-300">
              {currentAccessMembers.length === 0 ? (
                <div className="text-[9px] text-muted-foreground/45 italic py-0.5 px-1">No explicit members assigned</div>
              ) : (
                currentAccessMembers.map((access) => {
                  const profile = registry.memberMap[access.workspaceMemberId];
                  if (!profile) return null;

                  const name = profile.name || "Unknown User";
                  const initials = name.split(" ").map((w: string) => w[0]).slice(0, 2).join("").toUpperCase();
                  
                  const colors = ["bg-cyan-500", "bg-purple-500", "bg-indigo-500", "bg-teal-500", "bg-emerald-500", "bg-amber-500"];
                  const colorIdx = (initials.charCodeAt(0) + (initials.charCodeAt(1) || 0)) % colors.length;
                  const avatarBg = colors[colorIdx];

                  const isCreator = !!(space?.creatorId && access.workspaceMemberId === space.creatorId);

                  return (
                    <div key={access.workspaceMemberId} className="flex items-center gap-1.5 p-1 rounded-sm hover:bg-white/[0.02] transition-colors cursor-pointer group/member">
                      <div className={`h-4 w-4 rounded-full ${avatarBg} border border-border/20 flex items-center justify-center text-[8px] font-black text-white shrink-0`}>
                        {initials}
                      </div>
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
                      
                      <button
                        onClick={isCreator ? undefined : () => handleAccessChange(access.workspaceMemberId, access.accessLevel, false)}
                        disabled={isCreator}
                        className={`ml-auto text-[7.5px] uppercase tracking-widest font-black border border-border/25 px-1 py-0.5 rounded transition-all leading-none ${
                          isCreator
                            ? "text-primary border-primary/25 bg-primary/5 cursor-not-allowed opacity-80"
                            : "hover:bg-white/[0.04] cursor-pointer hover:border-primary/40 active:scale-95"
                        } ${
                          access.accessLevel === "Manager" && !isCreator
                            ? "text-rose-400 border-rose-500/25 bg-rose-500/5"
                            : access.accessLevel === "Editor" && !isCreator
                            ? "text-sky-400 border-sky-500/25 bg-sky-500/5"
                            : !isCreator
                            ? "text-emerald-400 border-emerald-500/25 bg-emerald-500/5"
                            : ""
                        }`}
                        title={isCreator ? "Owner has full management access" : "Click to toggle access privileges"}
                      >
                        {isCreator ? "Owner" : access.accessLevel}
                      </button>
                    </div>
                  );
                })
              )}
            </div>
          )}
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
