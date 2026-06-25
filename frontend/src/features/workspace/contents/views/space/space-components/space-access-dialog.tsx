import React, { useState, useCallback, useMemo } from "react";
import { useSelector } from "react-redux";
import { Shield, Save } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { useGetEntityAccessQuery, useUpdateEntityAccessMutation } from "../space-api";
import { entityAccessSelectors, memberSelectors } from "@/store/entityStore";
import type { AccessLevel } from "@/types/access-level";
import { UserAvatar } from "@/components/user-avatar";
import { RowAction } from "@/types/row-action";
import { toast } from "sonner";
import { cn } from "@/lib/utils";

interface SpaceAccessDialogProps {
  spaceId: string;
  trigger: React.ReactNode;
}

const LEVELS = ["None", "Viewer", "Editor", "Manager"] as const;
type Level = typeof LEVELS[number];

export function SpaceAccessDialog({ spaceId, trigger }: Readonly<SpaceAccessDialogProps>) {
  useGetEntityAccessQuery(spaceId);
  const entityAccessList = useSelector(entityAccessSelectors.selectAll).filter(ea => ea.spaceId === spaceId);
  const [updateEntityAccess, { isLoading: isSaving }] = useUpdateEntityAccessMutation();
  const allMembers = useSelector(memberSelectors.selectAll);

  // pending: memberId → chosen level (local only until save)
  const [pending, setPending] = useState<Map<string, Level>>(new Map());

  const accessByMember = useMemo(() => {
    const map = new Map<string, { id: string; level: AccessLevel }>();
    entityAccessList.forEach(ea => {
      if (ea.haveAccess) map.set(ea.workspaceMemberId, { id: ea.id, level: ea.accessLevel as AccessLevel });
    });
    return map;
  }, [entityAccessList]);

  const getDisplayLevel = useCallback((memberId: string): Level => {
    if (pending.has(memberId)) return pending.get(memberId)!;
    const ea = accessByMember.get(memberId);
    return ea ? ea.level as Level : "None";
  }, [pending, accessByMember]);

  // Sort: admins/owners first (they bypass entity access), then members with access, then the rest
  const sortedMembers = useMemo(() => {
    return [...allMembers].sort((a, b) => {
      const aPrivileged = a.role === "Owner" || a.role === "Admin";
      const bPrivileged = b.role === "Owner" || b.role === "Admin";
      if (aPrivileged && !bPrivileged) return -1;
      if (!aPrivileged && bPrivileged) return 1;
      const aHas = getDisplayLevel(a.id) !== "None";
      const bHas = getDisplayLevel(b.id) !== "None";
      if (aHas && !bHas) return -1;
      if (!aHas && bHas) return 1;
      return 0;
    });
  }, [allMembers, getDisplayLevel]);

  const dirtyCount = pending.size;

  const handleDiscard = useCallback(() => setPending(new Map()), []);

  const handleSave = useCallback(async () => {
    if (pending.size === 0 || isSaving) return;

    const rows: Parameters<typeof updateEntityAccess>[0]["rows"] = [];

    for (const [memberId, newLevel] of pending.entries()) {
      const existing = accessByMember.get(memberId);
      if (newLevel === "None" && existing) {
        rows.push({ id: existing.id, memberId, accessLevel: "Viewer", action: RowAction.Delete });
      } else if (newLevel !== "None" && !existing) {
        rows.push({ memberId, accessLevel: newLevel as AccessLevel, action: RowAction.Create });
      } else if (newLevel !== "None" && existing && existing.level !== newLevel) {
        rows.push({ id: existing.id, memberId, accessLevel: newLevel as AccessLevel, action: RowAction.Update });
      }
    }

    if (rows.length === 0) { setPending(new Map()); return; }

    try {
      await updateEntityAccess({ spaceId, rows }).unwrap();
      setPending(new Map());
    } catch (err) {
      const msg = (err as { data?: { Description?: string } })?.data?.Description;
      toast.error(msg || "Failed to save access changes.");
    }
  }, [pending, accessByMember, spaceId, updateEntityAccess, isSaving]);

  return (
    <Dialog>
      <DialogTrigger asChild>{trigger}</DialogTrigger>
      <DialogContent className="max-w-sm bg-background border border-border/30 rounded-md p-1.5 text-foreground">
        <DialogHeader className="p-1 pb-1.5 border-b border-border/20">
          <DialogTitle className="text-xs font-black text-foreground/90 flex items-center gap-1.5">
            <Shield className="h-3.5 w-3.5 text-primary" /> Space Access
          </DialogTitle>
        </DialogHeader>

        <div className="max-h-80 overflow-y-auto divide-y divide-border/10 [&::-webkit-scrollbar]:w-1 [&::-webkit-scrollbar-thumb]:bg-white/5">
          {sortedMembers.map(member => {
            const isPrivileged = member.role === "Owner" || member.role === "Admin";
            const displayLevel = isPrivileged ? "Manager" : getDisplayLevel(member.id);
            const isDirty = pending.has(member.id);

            return (
              <div
                key={member.id}
                className={cn(
                  "flex items-center gap-2.5 px-2 py-1.5 transition-colors",
                  isDirty ? "bg-primary/10 border-l-2 border-primary/25" : "hover:bg-muted/20"
                )}
              >
                <UserAvatar
                  name={member.name}
                  avatarUrl={member.avatarUrl}
                  className="h-6 w-6 rounded-md"
                  fallbackClassName="rounded-md text-[9px]"
                />

                <span className="flex-1 text-[11px] font-medium text-foreground/90 truncate">{member.name}</span>

                {isPrivileged ? (
                  <span className="text-[9px] font-black uppercase tracking-wider text-primary/60 bg-primary/10 px-1.5 py-0.5 rounded-md shrink-0">
                    {member.role}
                  </span>
                ) : (
                  <select
                    value={displayLevel}
                    disabled={isSaving}
                    onChange={e => {
                      const val = e.target.value as Level;
                      const stored = accessByMember.get(member.id)?.level ?? "None";
                      setPending(prev => {
                        const next = new Map(prev);
                        // If changed back to stored value, remove from pending
                        if (val === stored) next.delete(member.id);
                        else next.set(member.id, val);
                        return next;
                      });
                    }}
                    className={cn(
                      "bg-background border rounded-md px-1.5 py-0.5 text-[9px] font-bold outline-none cursor-pointer shrink-0 disabled:opacity-50 disabled:cursor-not-allowed",
                      displayLevel === "None"
                        ? "border-border/20 text-muted-foreground/50"
                        : "border-primary/25 text-primary"
                    )}
                  >
                    {LEVELS.map(l => <option key={l} value={l}>{l}</option>)}
                  </select>
                )}
              </div>
            );
          })}
        </div>

        {dirtyCount > 0 && (
          <div className="flex items-center justify-end gap-1.5 pt-1 border-t border-border/20">
            <Button variant="ghost" size="sm" onClick={handleDiscard} disabled={isSaving}
              className="h-6 px-2 text-[10px] font-bold uppercase tracking-tight text-muted-foreground">
              Discard
            </Button>
            <Button size="sm" onClick={handleSave} disabled={isSaving}
              className="h-6 px-2.5 text-[10px] font-bold uppercase tracking-tight gap-1">
              <Save className="h-3 w-3" />
              {isSaving ? "Saving..." : `Save ${dirtyCount} change${dirtyCount !== 1 ? "s" : ""}`}
            </Button>
          </div>
        )}
      </DialogContent>
    </Dialog>
  );
}
