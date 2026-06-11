import React from "react";
import { useSelector } from "react-redux";
import { Shield } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { useGetEntityAccessQuery, useUpdateEntityAccessMutation, useSpaceDetail } from "../space-api";
import { entityAccessSelectors, memberSelectors } from "@/store/entityStore";
import type { MemberRecord, EntityAccessRecord } from "@/types/workspace";
import type { AccessLevel } from "@/types/access-level";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";

interface SpaceAccessDialogProps {
  spaceId: string;
  trigger: React.ReactNode;
}

interface SpaceAccessDialogRowProps {
  member: MemberRecord;
  isCreator: boolean;
  access?: EntityAccessRecord;
  onUpdateAccess: (newLevel: AccessLevel | "None", action: "Create" | "Update" | "Delete") => void;
}

export function SpaceAccessDialogRow({ member, isCreator, access, onUpdateAccess }: Readonly<SpaceAccessDialogRowProps>) {
  const haveAccess = isCreator ? true : (access ? access.haveAccess : false);
  const currentLevel = isCreator ? "Manager" : (access && access.haveAccess ? access.accessLevel : "None");
  const initials = (member.name || "U").split(" ").map((w: string) => w[0]).slice(0, 2).join("").toUpperCase();

  const handleToggleCheckbox = () => {
    if (isCreator) return;
    const action = haveAccess ? "Delete" : "Create";
    const nextLevel = haveAccess ? "None" : "Viewer";
    onUpdateAccess(nextLevel, action);
  };

  const handleLevelSelect = (e: React.ChangeEvent<HTMLSelectElement>) => {
    if (isCreator) return;
    const newLevel = e.target.value as AccessLevel | "None";
    const action = newLevel === "None" ? "Delete" : (haveAccess ? "Update" : "Create");
    onUpdateAccess(newLevel, action);
  };

  return (
    <tr className="hover:bg-white/[0.01] transition-colors">
      <td className="p-1 text-center">
        <input
          type="checkbox"
          checked={haveAccess}
          onChange={handleToggleCheckbox}
          disabled={isCreator}
          className="h-3 w-3 rounded border-border/40 accent-primary cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed"
        />
      </td>
      <td className="p-1 flex items-center gap-1.5">
        <Avatar className="h-4 w-4 shrink-0 text-[8px] font-black">
          <AvatarImage src={member.avatarUrl} alt={member.name} />
          <AvatarFallback className="bg-primary/20 text-white border border-border/20 leading-none flex items-center justify-center">
            {initials}
          </AvatarFallback>
        </Avatar>
        <div>
          <div className="font-bold text-[10px] text-foreground/95 leading-none flex items-center gap-1">
            {member.name}
            {isCreator && (
              <span className="text-[7px] bg-primary/25 text-primary px-1 rounded-sm uppercase tracking-wider font-extrabold scale-90 origin-left">
                Owner
              </span>
            )}
          </div>
          <div className="text-[8px] text-muted-foreground/45 mt-0.5">{member.email}</div>
        </div>
      </td>
      <td className="p-1 text-[9px] font-mono uppercase text-muted-foreground/60 leading-none">
        {member.role || "Member"}
      </td>
      <td className="p-1 text-right pr-2">
        <select
          value={currentLevel}
          onChange={handleLevelSelect}
          disabled={isCreator}
          className="bg-background border border-border/25 rounded px-1 py-0.5 text-[9px] font-bold text-foreground/80 cursor-pointer disabled:cursor-not-allowed disabled:opacity-50 outline-none focus:border-primary/50 leading-none"
        >
          <option value="None">None (Default)</option>
          <option value="Viewer">Viewer</option>
          <option value="Editor">Editor</option>
          <option value="Manager">Manager</option>
        </select>
      </td>
    </tr>
  );
}

export function SpaceAccessDialog({ spaceId, trigger }: Readonly<SpaceAccessDialogProps>) {
  useGetEntityAccessQuery(spaceId);
  const space = useSpaceDetail(spaceId);
  const entityAccessList = useSelector(entityAccessSelectors.selectAll).filter(ea => ea.spaceId === spaceId);
  const [updateEntityAccess] = useUpdateEntityAccessMutation();
  const allWorkspaceMembers = useSelector(memberSelectors.selectAll);

  return (
    <Dialog>
      <DialogTrigger asChild>
        {trigger}
      </DialogTrigger>
      <DialogContent className="max-w-xl bg-background border border-border/30 rounded-md p-1.5 text-foreground">
        <DialogHeader className="p-1 pb-1.5 border-b border-border/20">
          <DialogTitle className="text-xs font-black text-foreground/90 flex items-center gap-1">
            <Shield className="h-3.5 w-3.5 text-primary" /> Manage Space Access Permissions
          </DialogTitle>
        </DialogHeader>
        
        <div className="mt-1 border border-border/20 rounded-md overflow-hidden bg-card/25">
          <div className="max-h-[300px] overflow-y-auto [&::-webkit-scrollbar]:w-1 [&::-webkit-scrollbar-thumb]:bg-white/[0.05]">
            <table className="w-full text-xs text-left">
              <thead className="bg-white/[0.02] border-b border-border/20 text-muted-foreground/75 font-mono text-[8px] uppercase tracking-wider">
                <tr>
                  <th className="p-1 font-semibold text-center w-8">Access</th>
                  <th className="p-1 font-semibold">Workspace Member</th>
                  <th className="p-1 font-semibold">Role</th>
                  <th className="p-1 font-semibold text-right pr-2">Space Privilege</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border/10 text-muted-foreground/95">
                {allWorkspaceMembers.map((member) => {
                  const isCreator = !!(space?.creatorId && (member.id === space.creatorId || member.workspaceMemberId === space.creatorId));
                  const access = entityAccessList.find(a => a.workspaceMemberId === member.id || a.workspaceMemberId === member.workspaceMemberId);

                  return (
                    <SpaceAccessDialogRow
                      key={member.id || member.workspaceMemberId}
                      member={member}
                      isCreator={isCreator}
                      access={access}
                      onUpdateAccess={async (newLevel, action) => {
                        await updateEntityAccess({
                          spaceId,
                          rows: [{
                            id: access?.id,
                            memberId: member.id || member.workspaceMemberId || "",
                            accessLevel: newLevel === "None" ? "Viewer" : newLevel,
                            action
                          }]
                        }).unwrap();
                      }}
                    />
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
