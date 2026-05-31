import { useSpaceDetail, useGetEntityAccessQuery, useUpdateEntityAccessMutation, useSpaceStatuses, useSpaceBoardItems } from "../space-api";
import {
  PenBox,
  FileText,
  Plus,
  Shield,
  Activity,
  TrendingUp,
  X
} from "lucide-react";
import React, { useState, useMemo } from "react";
import { StatusBadge } from "@/components/status-badge";
import { useWorkspace } from "@/features/workspace/context/workspace-provider";
import { DescriptionSection } from "../../../layer-detail/components/overview/description-section";
import { useSelector } from "react-redux";
import { entityAccessSelectors } from "@/store/entityStore";
import { SpaceAccessDialog } from "./space-access-dialog";
import { CreateStatusForm } from "@/features/workspace/components/forms/create-status-form";

interface SpaceDetailProps {
  spaceId: string;
}

interface MockUpdate {
  id: string;
  authorName: string;
  avatarInitials: string;
  avatarBg: string;
  timestamp: string;
  health: "On Track" | "At Risk" | "Blocked";
  content: string;
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

  // Concept of project updates: Local state so the user can interactively write & view updates instantly
  const [updates, setUpdates] = useState<MockUpdate[]>([
    {
      id: "1",
      authorName: "Anh Đức Nguyễn",
      avatarInitials: "AD",
      avatarBg: "bg-cyan-500",
      timestamp: "Today at 2:40 PM",
      health: "On Track",
      content: "Completed structural migration of space boards. The columns now render fully dynamic backdrops."
    },
    {
      id: "2",
      authorName: "John Doe",
      avatarInitials: "JD",
      avatarBg: "bg-purple-500",
      timestamp: "Yesterday at 11:15 AM",
      health: "At Risk",
      content: "Evaluating backend memory pressure under concurrent Dapper joins. Testing index configurations."
    }
  ]);
  
  const [isWritingUpdate, setIsWritingUpdate] = useState(false);
  const [newUpdateContent, setNewUpdateContent] = useState("");
  const [newUpdateHealth, setNewUpdateHealth] = useState<"On Track" | "At Risk" | "Blocked">("On Track");

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

  // Handle submitting new mockup status updates
  const handlePostUpdate = (e: React.FormEvent) => {
    e.preventDefault();
    if (!newUpdateContent.trim()) return;

    const newUpdate: MockUpdate = {
      id: Date.now().toString(),
      authorName: "Anh Đức Nguyễn",
      avatarInitials: "AD",
      avatarBg: "bg-cyan-500",
      timestamp: "Just now",
      health: newUpdateHealth,
      content: newUpdateContent
    };

    setUpdates([newUpdate, ...updates]);
    setNewUpdateContent("");
    setIsWritingUpdate(false);
  };


  return (
    <div className="flex h-full w-full bg-transparent overflow-hidden text-foreground">
      
      {/* 1. Left Column: Main Details Canvas */}
      <div className="flex-1 overflow-y-auto p-2 space-y-3 relative [&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-thumb]:bg-white/[0.05] [&::-webkit-scrollbar-thumb]:rounded-full hover:[&::-webkit-scrollbar-thumb]:bg-white/[0.15]">
        
        {/* Document Panel */}
        <div className="rounded-xl border border-border/25 overflow-hidden bg-card/10 shadow-sm">
          {/* Tab bar */}
          <div className="flex items-center gap-0 bg-white/[0.02] border-b border-border/15 px-2 pt-1 select-none">
            {/* Default doc tab — always present */}
            <div
              className="flex items-center gap-1.5 pb-1.5 px-2 text-[11px] font-semibold border-b-2 border-primary text-foreground cursor-default"
            >
              <FileText className="h-3 w-3 text-primary" />
              <span>Overview</span>
            </div>

            {/* Future tabs placeholder */}
            <button
              className="ml-auto mb-1.5 flex items-center gap-1 px-1.5 py-0.5 rounded-md text-[10px] text-muted-foreground/40 hover:text-muted-foreground hover:bg-muted/40 transition-all cursor-pointer"
              title="Add document (coming soon)"
            >
              <Plus className="h-3 w-3" />
            </button>
          </div>

          {/* Document content */}
          <div className="px-4 py-3">
            {space.defaultDocumentId ? (
              <DescriptionSection documentId={space.defaultDocumentId} />
            ) : (
              <div className="flex flex-col items-center justify-center py-10 text-center gap-2">
                <FileText className="h-8 w-8 text-muted-foreground/20" />
                <p className="text-xs text-muted-foreground/50 font-medium">No document available for this space yet.</p>
              </div>
            )}
          </div>
        </div>

        {/* Project Update Box */}
        {!isWritingUpdate ? (
          <div
            onClick={() => setIsWritingUpdate(true)}
            className="w-full p-2.5 border border-border/30 rounded-lg bg-card/25 hover:bg-card/45 cursor-pointer flex items-center justify-center gap-2 text-muted-foreground/75 hover:text-foreground transition-all duration-200 select-none group/update shadow-sm"
          >
            <PenBox className="h-3.5 w-3.5 opacity-75 group-hover/update:scale-105 transition-transform" />
            <span className="text-[11px] font-bold">Write project update</span>
          </div>
        ) : (
          <form onSubmit={handlePostUpdate} className="p-3 border border-border/40 rounded-xl bg-[#161618] space-y-3 animate-in fade-in-50 duration-150">
            <div className="flex items-center justify-between">
              <span className="text-[11px] font-bold text-foreground/80 flex items-center gap-1">
                <TrendingUp className="h-3.5 w-3.5 text-sky-400" /> New Project Update
              </span>
              <button
                type="button"
                onClick={() => setIsWritingUpdate(false)}
                className="text-muted-foreground/50 hover:text-foreground transition-colors p-0.5 rounded cursor-pointer"
              >
                <X className="h-3.5 w-3.5" />
              </button>
            </div>

            <textarea
              value={newUpdateContent}
              onChange={(e) => setNewUpdateContent(e.target.value)}
              placeholder="What did your team accomplish? Any current risks or blockers?"
              className="w-full h-16 bg-black/35 border border-border/25 rounded-md p-2 text-xs text-foreground placeholder:text-muted-foreground/40 focus:outline-none focus:border-primary/50 resize-none font-sans"
            />

            <div className="flex items-center justify-between">
              <div className="flex items-center gap-1.5">
                <span className="text-[10px] text-muted-foreground/60 font-medium">Health:</span>
                {(["On Track", "At Risk", "Blocked"] as const).map((h) => (
                  <button
                    key={h}
                    type="button"
                    onClick={() => setNewUpdateHealth(h)}
                    className={`px-2 py-0.5 rounded text-[9px] font-bold cursor-pointer transition-all border ${
                      newUpdateHealth === h
                        ? h === "On Track"
                          ? "bg-emerald-500/10 border-emerald-500/40 text-emerald-400"
                          : h === "At Risk"
                          ? "bg-amber-500/10 border-amber-500/40 text-amber-400"
                          : "bg-rose-500/10 border-rose-500/40 text-rose-400"
                        : "bg-transparent border-border/20 text-muted-foreground/45 hover:text-muted-foreground"
                    }`}
                  >
                    {h}
                  </button>
                ))}
              </div>

              <button
                type="submit"
                disabled={!newUpdateContent.trim()}
                className="h-6 px-3 rounded-md bg-primary text-primary-foreground text-[10px] font-bold hover:bg-primary/95 disabled:opacity-45 disabled:pointer-events-none transition-all cursor-pointer"
              >
                Post Update
              </button>
            </div>
          </form>
        )}

        {/* Dynamic Project Updates Timeline */}
        <div className="space-y-2.5">
          <h4 className="text-xs font-bold text-foreground/80 px-0.5 flex items-center gap-1">
            <Activity className="h-3.5 w-3.5 text-muted-foreground/60" /> Latest Updates
          </h4>
          <div className="relative border-l border-border/30 ml-2.5 pl-3.5 space-y-3">
            {updates.map((up) => (
              <div key={up.id} className="relative group/timeline animate-in slide-in-from-top-1 duration-200">
                {/* Timeline Bullet */}
                <div className={`absolute -left-[19.5px] top-1.5 h-2 w-2 rounded-full border border-black shrink-0 ${
                  up.health === "On Track" ? "bg-emerald-500" : up.health === "At Risk" ? "bg-amber-500" : "bg-rose-500"
                }`} />

                <div className="p-3 rounded-xl border border-border/20 bg-card/10 space-y-2 hover:border-border/35 hover:bg-card/20 transition-all duration-150">
                  <div className="flex items-center gap-2">
                    <div className={`h-5 w-5 rounded-full ${up.avatarBg} border border-border/20 flex items-center justify-center text-[9px] font-black text-white shrink-0`}>
                      {up.avatarInitials}
                    </div>
                    <span className="font-bold text-[11px] text-foreground/85">{up.authorName}</span>
                    <span className="text-[9px] text-muted-foreground/50">{up.timestamp}</span>

                    <span className={`text-[8px] uppercase tracking-wider font-extrabold px-1.5 py-0.5 rounded ml-auto border ${
                      up.health === "On Track"
                        ? "bg-emerald-500/10 border-emerald-500/20 text-emerald-400"
                        : up.health === "At Risk"
                        ? "bg-amber-500/10 border-amber-500/20 text-amber-400"
                        : "bg-rose-500/10 border-rose-500/20 text-rose-400"
                    }`}>
                      {up.health}
                    </span>
                  </div>
                  <p className="text-xs text-muted-foreground/85 leading-relaxed font-sans pl-7">
                    {up.content}
                  </p>
                </div>
              </div>
            ))}
          </div>
        </div>

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
