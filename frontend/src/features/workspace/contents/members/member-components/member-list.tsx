"use client";

import { useState, useMemo } from "react";
import type { MemberRecord } from "@/types/workspace/member-record";
import type { Role } from "@/types/role";
import { ROLE_LABELS } from "@/types/role";
import type { MembershipStatus } from "@/types/membership-status";
import { MEMBERSHIP_STATUS_LABELS } from "@/types/membership-status";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Checkbox } from "@/components/ui/checkbox";
import { UserAvatar } from "@/components/user-avatar";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuTrigger,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
} from "@/components/ui/dropdown-menu";
import { RoleBadge } from "@/components/role-badge";
import { Save, Plus, Trash2, Search, Undo2, Check, X } from "lucide-react";
import { cn } from "@/lib/utils";

// ─── Types ───────────────────────────────────────────────────────────────────

type PendingAdd = { tempId: string; email: string; role: Role };
type DirtyUpdate = { role?: Role; status?: MembershipStatus };

export interface MemberSavePayload {
  adds: { email: string; role: Role }[];
  updates: { memberId: string; role?: Role; status?: MembershipStatus }[];
  removes: string[];
}

interface Props {
  members?: MemberRecord[];
  currentUserId?: string;
  isSaving?: boolean;
  onSave: (payload: MemberSavePayload) => Promise<void>;
}

// ─── Constants ────────────────────────────────────────────────────────────────

const ASSIGNABLE_ROLES = (Object.keys(ROLE_LABELS) as Role[]).filter(
  (r) => r !== "None" && r !== "Owner",
);
const ASSIGNABLE_STATUSES = (Object.keys(MEMBERSHIP_STATUS_LABELS) as MembershipStatus[]).filter(
  (s) => s === "Active" || s === "Suspended",
);
const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

// ─── Status pill (mirrors StatusBadge pill variant) ──────────────────────────

const STATUS_COLORS: Record<MembershipStatus, { bg: string; text: string }> = {
  Active:    { bg: "bg-emerald-500/15", text: "text-emerald-400" },
  Suspended: { bg: "bg-red-500/15",     text: "text-red-400"     },
  Pending:   { bg: "bg-amber-500/15",   text: "text-amber-400"   },
  Invited:   { bg: "bg-blue-500/15",    text: "text-blue-400"    },
};

function StatusPill({ status, className }: { status: MembershipStatus, className?: string }) {
  const { bg, text } = STATUS_COLORS[status] ?? STATUS_COLORS.Active;
  return (
    <span className={cn("inline-flex items-center h-5 px-2 rounded-md text-[10px] font-semibold", bg, text, className)}>
      {status}
    </span>
  );
}

// ─── Theme Background Hierarchy Standardization ─────────────────────────────

// Standardize bg-background, bg-card, bg-muted, and bg-popover across the app
// to ensure proper layout depth and preparation for future theme-switching.

// ─── Component ────────────────────────────────────────────────────────────────

export function MemberList({ members = [], currentUserId, isSaving, onSave }: Props) {
  const [searchQuery, setSearchQuery]           = useState("");
  const [selectedIds, setSelectedIds]           = useState<Set<string>>(new Set());
  const [pendingUpdates, setPendingUpdates]     = useState<Map<string, DirtyUpdate>>(new Map());
  const [pendingRemoves, setPendingRemoves]     = useState<Set<string>>(new Set());
  const [pendingAdds, setPendingAdds]           = useState<PendingAdd[]>([]);
  const [addEmail, setAddEmail]                 = useState("");
  const [addRole, setAddRole]                   = useState<Role>("Guest");
  const [batchRole, setBatchRole]               = useState<Role | "">("");
  const [batchStatus, setBatchStatus]           = useState<MembershipStatus | "">("");

  const dirtyCount = pendingUpdates.size + pendingRemoves.size + pendingAdds.length;

  const filteredMembers = useMemo(() => {
    const q = searchQuery.toLowerCase();
    const matched = !searchQuery
      ? members
      : members.filter(
          (m) => m.name.toLowerCase().includes(q) || (m.email ?? "").toLowerCase().includes(q),
        );
    // Owner always pinned to the top — stable sort, so relative order among everyone else
    // is untouched.
    return [...matched].sort((a, b) => (a.role === "Owner" ? -1 : b.role === "Owner" ? 1 : 0));
  }, [members, searchQuery]);

  const selectableMembers = filteredMembers.filter((m) => m.role !== "Owner");
  const allVisibleSelected =
    selectableMembers.length > 0 &&
    selectableMembers.every((m) => selectedIds.has(m.id));

  // ─── Selection ────────────────────────────────────────────────────────────

  const toggleSelect = (id: string) => {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const toggleSelectAll = () => {
    if (allVisibleSelected) {
      setSelectedIds(new Set());
    } else {
      setSelectedIds(new Set(selectableMembers.map((m) => m.id)));
    }
  };

  const clearSelection = () => {
    setSelectedIds(new Set());
    setBatchRole("");
    setBatchStatus("");
  };

  // ─── Inline edit ──────────────────────────────────────────────────────────

  const markUpdate = (memberId: string, patch: DirtyUpdate) => {
    setPendingUpdates((prev) => {
      const next = new Map(prev);
      next.set(memberId, { ...(next.get(memberId) ?? {}), ...patch });
      return next;
    });
  };

  const toggleRemove = (memberId: string) => {
    setPendingRemoves((prev) => {
      const next = new Set(prev);
      if (next.has(memberId)) next.delete(memberId);
      else next.add(memberId);
      return next;
    });
    setPendingUpdates((prev) => {
      if (!prev.has(memberId)) return prev;
      const next = new Map(prev);
      next.delete(memberId);
      return next;
    });
  };

  // ─── Batch (floating bar) ─────────────────────────────────────────────────

  const applyBatch = () => {
    if (!batchRole && !batchStatus) return;
    selectedIds.forEach((id) => {
      const patch: DirtyUpdate = {};
      if (batchRole)   patch.role   = batchRole as Role;
      if (batchStatus) patch.status = batchStatus as MembershipStatus;
      markUpdate(id, patch);
    });
    clearSelection();
  };

  const batchRemove = () => {
    selectedIds.forEach((id) => {
      if (id === currentUserId) return; // never queue self-removal
      setPendingRemoves((prev) => new Set([...prev, id]));
    });
    clearSelection();
  };

  // ─── Add row ──────────────────────────────────────────────────────────────

  const commitAddRow = () => {
    const email = addEmail.trim();
    if (!EMAIL_RE.test(email)) return;
    if (pendingAdds.some((a) => a.email === email)) { setAddEmail(""); return; }
    setPendingAdds((prev) => [...prev, { tempId: crypto.randomUUID(), email, role: addRole }]);
    setAddEmail("");
  };

  const removePendingAdd = (tempId: string) =>
    setPendingAdds((prev) => prev.filter((a) => a.tempId !== tempId));

  const updatePendingAddRole = (tempId: string, role: Role) =>
    setPendingAdds((prev) => prev.map((a) => (a.tempId === tempId ? { ...a, role } : a)));

  // ─── Save / discard ───────────────────────────────────────────────────────

  const handleSave = async () => {
    if (dirtyCount === 0 || isSaving) return;
    await onSave({
      adds: pendingAdds.map(({ email, role }) => ({ email, role })),
      updates: [...pendingUpdates.entries()].map(([memberId, patch]) => ({ memberId, ...patch })),
      removes: [...pendingRemoves],
    });
    setPendingUpdates(new Map());
    setPendingRemoves(new Set());
    setPendingAdds([]);
  };

  const handleDiscard = () => {
    setPendingUpdates(new Map());
    setPendingRemoves(new Set());
    setPendingAdds([]);
    setAddEmail("");
  };

  // ─── Render ───────────────────────────────────────────────────────────────

  return (
    <div className="h-full flex flex-col bg-card/40 overflow-hidden relative">

      {/* Header */}
      <div className="border-b border-border px-2 py-1 flex items-center gap-2">
        <div className="flex items-center gap-2 px-2 h-7 rounded-md bg-secondary/60 border border-transparent focus-within:border-primary/30 focus-within:bg-secondary transition-all group flex-1 max-w-xs shadow-inner">
          <Search className="h-3 w-3 text-muted-foreground/40 group-focus-within:text-primary transition-colors shrink-0" />
          <input
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder="Search members..."
            className="flex-1 bg-transparent border-none outline-none text-[11px] font-medium text-foreground placeholder:text-muted-foreground/40 transition-all min-w-0"
          />
          {searchQuery && (
            <button
              onClick={() => setSearchQuery("")}
              className="text-muted-foreground/40 hover:text-foreground transition-colors shrink-0"
            >
              <X className="h-3 w-3" />
            </button>
          )}
        </div>

        <div className="ml-auto flex items-center gap-1.5">
          {dirtyCount > 0 && (
            <>
              <Button
                variant="ghost"
                size="sm"
                onClick={handleDiscard}
                disabled={isSaving}
                className="h-7 px-2 rounded-md text-[10px] font-bold uppercase tracking-tight text-muted-foreground"
              >
                Discard
              </Button>
              <Button
                size="sm"
                onClick={handleSave}
                disabled={isSaving}
                className="h-7 px-3 rounded-md text-[10px] font-bold uppercase tracking-tight gap-1.5"
              >
                <Save className="h-3 w-3" />
                {isSaving ? "Saving..." : `Save ${dirtyCount} change${dirtyCount !== 1 ? "s" : ""}`}
              </Button>
            </>
          )}
        </div>
      </div>

      {/* Table */}
      <div className="flex-1 overflow-auto">
        <div className="min-w-[700px]">
          {/* Column headers */}
          <div className="grid grid-cols-[28px_3fr_140px_140px_80px_36px] text-[10px] font-black uppercase tracking-wider text-muted-foreground/80 border-b border-border bg-muted/30 sticky top-0 z-10">
            <div className="flex items-center justify-center py-2">
              <Checkbox
                className="rounded-md h-3.5 w-3.5"
                checked={allVisibleSelected}
                onCheckedChange={toggleSelectAll}
              />
            </div>
            <div className="px-3 py-2">Member</div>
            <div className="px-3 py-2">Role</div>
            <div className="px-3 py-2">Status</div>
            <div className="px-3 py-2">Joined</div>
            <div />
          </div>

          <div className="divide-y divide-border">
          {/* Existing members */}
          {filteredMembers.map((member) => {
            const isRemoved   = pendingRemoves.has(member.id);
            const isDirty     = pendingUpdates.has(member.id);
            const isSelected  = selectedIds.has(member.id);
            const isSelf      = member.userId === currentUserId;
            const isOwner     = member.role === "Owner";
            const patch       = pendingUpdates.get(member.id) ?? {};
            const displayRole = patch.role ?? member.role ?? "Guest";
            const displayStatus = patch.status ?? member.status ?? "Active";

            return (
              <div
                key={member.id}
                className={cn(
                  "grid grid-cols-[28px_3fr_140px_140px_80px_36px] items-center text-[11px] transition-colors",
                  isRemoved  ? "bg-destructive/5 border-l-2 border-destructive/30 opacity-50"
                  : isDirty  ? "bg-primary/10 border-l-2 border-primary/25"
                  : isSelected ? "bg-primary/5"
                  : "hover:bg-muted/5",
                )}
              >
                {/* Checkbox */}
                <div className="flex items-center justify-center py-2">
                  <Checkbox
                    className="rounded-md h-3.5 w-3.5"
                    checked={isSelected}
                    onCheckedChange={() => toggleSelect(member.id)}
                    disabled={isRemoved || isOwner}
                  />
                </div>

                {/* Name / Email */}
                <div className="flex items-center gap-2.5 px-3 py-2 min-w-0">
                  <UserAvatar
                    name={member.name}
                    avatarUrl={member.avatarUrl}
                    className="h-6 w-6 rounded-md"
                    fallbackClassName="text-[9px] rounded-md"
                  />
                  <div className="flex flex-col min-w-0">
                    <div className="flex items-center gap-1.5">
                      <span className={cn("font-medium truncate", isRemoved && "line-through text-muted-foreground")}>
                        {member.name}
                      </span>
                      {isSelf && (
                        <span className="text-[8px] font-black uppercase tracking-wider text-primary/50 bg-primary/10 px-1 rounded-md leading-4">
                          You
                        </span>
                      )}
                    </div>
                    <span className="text-muted-foreground/50 text-[10px] truncate">{member.email}</span>
                  </div>
                </div>

                {/* Role */}
                <div className="px-2 py-1.5">
                  {isOwner ? (
                    <RoleBadge role={displayRole} />
                  ) : (
                    <DropdownMenu>
                      <DropdownMenuTrigger asChild disabled={isRemoved}>
                        <button
                          type="button"
                          className="cursor-pointer focus:outline-none hover:opacity-80 transition-opacity disabled:opacity-50 disabled:cursor-default"
                        >
                          <RoleBadge role={displayRole} />
                        </button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent align="start" className="w-32 rounded-md p-0 overflow-hidden" onClick={(e) => e.stopPropagation()}>
                        <DropdownMenuRadioGroup value={displayRole} onValueChange={(val) => markUpdate(member.id, { role: val as Role })}>
                          {ASSIGNABLE_ROLES.map((r) => (
                            <DropdownMenuRadioItem
                              key={r}
                              value={r}
                              className={cn("cursor-pointer", displayRole === r && "bg-muted shadow-sm")}
                            >
                              <RoleBadge role={r} className="w-full justify-start border-none bg-transparent hover:bg-transparent p-0 h-auto" />
                            </DropdownMenuRadioItem>
                          ))}
                        </DropdownMenuRadioGroup>
                      </DropdownMenuContent>
                    </DropdownMenu>
                  )}
                </div>

                {/* Status */}
                <div className="px-2 py-1.5">
                  {isOwner ? (
                    <StatusPill status={displayStatus as MembershipStatus} />
                  ) : (
                    <DropdownMenu>
                      <DropdownMenuTrigger asChild disabled={isRemoved}>
                        <button
                          type="button"
                          className="cursor-pointer focus:outline-none hover:opacity-80 transition-opacity disabled:opacity-50 disabled:cursor-default"
                        >
                          <StatusPill status={displayStatus as MembershipStatus} />
                        </button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent align="start" className="w-36 rounded-md p-0 overflow-hidden" onClick={(e) => e.stopPropagation()}>
                        <DropdownMenuRadioGroup value={displayStatus} onValueChange={(val) => markUpdate(member.id, { status: val as MembershipStatus })}>
                          {ASSIGNABLE_STATUSES.map((s) => (
                            <DropdownMenuRadioItem
                              key={s}
                              value={s}
                              className={cn("cursor-pointer", displayStatus === s && "bg-muted shadow-sm")}
                            >
                              <StatusPill status={s} className="w-full justify-start border-none bg-transparent hover:bg-transparent p-0 h-auto" />
                            </DropdownMenuRadioItem>
                          ))}
                        </DropdownMenuRadioGroup>
                      </DropdownMenuContent>
                    </DropdownMenu>
                  )}
                </div>

                {/* Joined */}
                <div className="px-3 py-2 text-muted-foreground/40 text-[10px]">
                  {member.joinedAt
                    ? new Date(member.joinedAt).toLocaleDateString("en-US", { month: "short", day: "numeric" })
                    : "Invited"}
                </div>

                {/* Delete / Undo — hidden for self and the Owner */}
                <div className="flex items-center justify-center">
                  {!isSelf && !isOwner && (
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => toggleRemove(member.id)}
                      className={cn(
                        "h-6 w-6 p-0 rounded-md",
                        isRemoved
                          ? "text-amber-500 hover:text-foreground hover:bg-muted"
                          : "text-muted-foreground/20 hover:text-destructive hover:bg-destructive/10",
                      )}
                    >
                      {isRemoved ? <Undo2 className="h-3 w-3" /> : <Trash2 className="h-3 w-3" />}
                    </Button>
                  )}
                </div>
              </div>
            );
          })}

          {/* Pending adds */}
          {pendingAdds.map((add) => (
            <div
              key={add.tempId}
              className="grid grid-cols-[28px_3fr_140px_140px_80px_36px] items-center text-[11px] bg-primary/5 border-l-2 border-primary/30"
            >
              <div />
              <div className="flex items-center gap-2.5 px-3 py-2 min-w-0">
                <div className="h-6 w-6 rounded-md bg-primary/10 flex items-center justify-center shrink-0">
                  <Plus className="h-3 w-3 text-primary/40" />
                </div>
                <div className="flex flex-col min-w-0">
                  <span className="font-medium text-primary/80 truncate">{add.email}</span>
                  <span className="text-[9px] text-primary/40 font-mono uppercase tracking-wider">Pending invite</span>
                </div>
              </div>

              <div className="px-2 py-1.5">
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <button type="button" className="cursor-pointer focus:outline-none hover:opacity-80 transition-opacity">
                      <RoleBadge role={add.role} />
                    </button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="start" className="w-32 rounded-md p-0 overflow-hidden">
                    <DropdownMenuRadioGroup value={add.role} onValueChange={(val) => updatePendingAddRole(add.tempId, val as Role)}>
                      {ASSIGNABLE_ROLES.map((r) => (
                        <DropdownMenuRadioItem
                          key={r}
                          value={r}
                          className={cn("cursor-pointer", add.role === r && "bg-muted shadow-sm")}
                        >
                          <RoleBadge role={r} className="w-full justify-start border-none bg-transparent hover:bg-transparent p-0 h-auto" />
                        </DropdownMenuRadioItem>
                      ))}
                    </DropdownMenuRadioGroup>
                  </DropdownMenuContent>
                </DropdownMenu>
              </div>

              <div className="px-3 py-2 text-muted-foreground/30 text-[9px] font-mono">—</div>
              <div className="px-3 py-2 text-muted-foreground/30 text-[9px] font-mono">—</div>

              <div className="flex items-center justify-center">
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => removePendingAdd(add.tempId)}
                  className="h-6 w-6 p-0 rounded-md text-muted-foreground/20 hover:text-destructive hover:bg-destructive/10"
                >
                  <Trash2 className="h-3 w-3" />
                </Button>
              </div>
            </div>
          ))}

          {/* Add row */}
          <div className="grid grid-cols-[28px_3fr_140px_140px_80px_36px] items-center border-t border-dashed border-border/50 bg-muted/5 hover:bg-muted/10 transition-colors group">
            <div />
            <div className="flex items-center gap-2.5 px-3 py-1.5">
              <div className="h-6 w-6 rounded-md border border-dashed border-border/30 group-focus-within:border-primary/40 flex items-center justify-center shrink-0 transition-colors">
                <Plus className="h-3 w-3 text-muted-foreground/20 group-focus-within:text-primary/40" />
              </div>
              <Input
                placeholder="Add by email and press Enter..."
                value={addEmail}
                onChange={(e) => setAddEmail(e.target.value)}
                onKeyDown={(e) => { if (e.key === "Enter") { e.preventDefault(); commitAddRow(); } }}
                onBlur={commitAddRow}
                className="h-6 border-none bg-transparent text-[11px] px-0 placeholder:text-muted-foreground/25 focus-visible:ring-0 shadow-none"
              />
            </div>

            <div className="px-2 py-1.5">
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <button type="button" className="cursor-pointer focus:outline-none hover:opacity-80 transition-opacity">
                    <RoleBadge role={addRole} />
                  </button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="start" className="w-32 rounded-md p-0 overflow-hidden">
                  <DropdownMenuRadioGroup value={addRole} onValueChange={(val) => setAddRole(val as Role)}>
                    {ASSIGNABLE_ROLES.map((r) => (
                      <DropdownMenuRadioItem
                        key={r}
                        value={r}
                        className={cn("cursor-pointer", addRole === r && "bg-muted shadow-sm")}
                      >
                        <RoleBadge role={r} className="w-full justify-start border-none bg-transparent hover:bg-transparent p-0 h-auto" />
                      </DropdownMenuRadioItem>
                    ))}
                  </DropdownMenuRadioGroup>
                </DropdownMenuContent>
              </DropdownMenu>
            </div>

            <div /><div /><div />
          </div>
        </div>
        </div>
      </div>

      {/* Floating multi-select action bar */}
      {selectedIds.size > 0 && (
        <div className="absolute bottom-6 left-1/2 -translate-x-1/2 z-50 animate-in fade-in slide-in-from-bottom-4 duration-200">
          <div className="flex items-center gap-2 px-2.5 py-1.5 rounded-md bg-card/95 backdrop-blur-xl border border-border shadow-lg">
            {/* Count */}
            <div className="flex items-center gap-1 px-2 py-1 rounded-md bg-primary/10 border border-primary/20 h-7">
              <span className="text-[11px] font-black font-mono text-primary">{selectedIds.size}</span>
              <span className="text-[9px] font-black font-mono text-primary uppercase tracking-wider">Selected</span>
            </div>

            <div className="h-5 w-px bg-border/50" />

            {/* Batch role */}
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <button
                  type="button"
                  className="h-7 px-2 rounded-md bg-background/50 border border-border/50 hover:bg-accent text-[10px] font-medium transition-colors flex items-center gap-1.5"
                >
                  {batchRole ? <RoleBadge role={batchRole as Role} /> : <span className="text-muted-foreground">Role</span>}
                </button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="start" className="w-32 rounded-md p-0 overflow-hidden">
                <DropdownMenuRadioGroup value={batchRole} onValueChange={(val) => setBatchRole(val as Role)}>
                  {ASSIGNABLE_ROLES.map((r) => (
                    <DropdownMenuRadioItem
                      key={r}
                      value={r}
                      className={cn("cursor-pointer", batchRole === r && "bg-muted shadow-sm")}
                    >
                      <RoleBadge role={r} className="w-full justify-start border-none bg-transparent hover:bg-transparent p-0 h-auto" />
                    </DropdownMenuRadioItem>
                  ))}
                </DropdownMenuRadioGroup>
              </DropdownMenuContent>
            </DropdownMenu>

            {/* Batch status */}
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <button
                  type="button"
                  className="h-7 px-2 rounded-md bg-background/50 border border-border/50 hover:bg-accent text-[10px] font-medium transition-colors flex items-center gap-1.5"
                >
                  {batchStatus
                    ? <StatusPill status={batchStatus as MembershipStatus} />
                    : <span className="text-muted-foreground">Status</span>}
                </button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="start" className="w-36 rounded-md p-0 overflow-hidden">
                <DropdownMenuRadioGroup value={batchStatus} onValueChange={(val) => setBatchStatus(val as MembershipStatus)}>
                  {ASSIGNABLE_STATUSES.map((s) => (
                    <DropdownMenuRadioItem
                      key={s}
                      value={s}
                      className={cn("cursor-pointer", batchStatus === s && "bg-muted shadow-sm")}
                    >
                      <StatusPill status={s} className="w-full justify-start border-none bg-transparent hover:bg-transparent p-0 h-auto" />
                    </DropdownMenuRadioItem>
                  ))}
                </DropdownMenuRadioGroup>
              </DropdownMenuContent>
            </DropdownMenu>

            {(batchRole || batchStatus) && (
              <Button
                size="sm"
                onClick={applyBatch}
                className="h-7 px-3 rounded-md text-[10px] font-bold uppercase tracking-tight gap-1"
              >
                <Check className="h-3 w-3" />
                Apply
              </Button>
            )}

            <div className="h-5 w-px bg-border/50" />

            {/* Batch delete */}
            <Button
              size="sm"
              variant="outline"
              onClick={batchRemove}
              className="h-7 w-7 p-0 rounded-md bg-background/50 border-border/50 hover:bg-destructive/10 hover:text-destructive hover:border-destructive/30 transition-all"
              title="Remove selected"
            >
              <Trash2 className="h-3.5 w-3.5" />
            </Button>

            {/* Clear */}
            <Button
              size="sm"
              variant="ghost"
              onClick={clearSelection}
              className="h-7 w-7 p-0 rounded-md hover:bg-muted"
            >
              <X className="h-3.5 w-3.5 text-muted-foreground/50" />
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
