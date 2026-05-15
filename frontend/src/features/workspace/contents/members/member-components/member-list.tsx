import { useMemo, useState } from "react";
import type { MemberSummary } from "../members-type";
import { Button } from "@/components/ui/button";
import {
  Filter,
  MoreVertical,
  Plus,
  Search,
  Check,
  Trash2,
  X,
} from "lucide-react";
import { Input } from "@/components/ui/input";
import {
  DropdownMenu,
  DropdownMenuCheckboxItem,
  DropdownMenuContent,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  DropdownMenuItem,
} from "@/components/ui/dropdown-menu";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Checkbox } from "@/components/ui/checkbox";
import { RoleBadge } from "@/components/role-badge";

import type { Role } from "@/types/role";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import type { MembershipStatus } from "@/types/membership-status";

interface Props {
  members?: MemberSummary[];
  onAddMember?: () => void;
  onDeleteMember?: (id: string) => void;
  onBatchUpdate?: (ids: string[], role?: Role, status?: MembershipStatus) => void;
  onBatchDelete?: (ids: string[]) => void;
}

export function MemberList({
  members,
  onAddMember,
  onDeleteMember,
  onBatchUpdate,
  onBatchDelete,
}: Props) {
  const [searchQuery, setSearchQuery] = useState("");
  const [selectedRoles, setSelectedRoles] = useState<string[]>([]);
  const [selectedMembers, setSelectedMembers] = useState<string[]>([]);
  const [selectedRole, setSelectedRole] = useState<Role | "">("");
  const [selectedStatus, setSelectedStatus] = useState<MembershipStatus | "">(
    "",
  );

  const toggleMemberSelection = (id: string) => {
    setSelectedMembers((prev) =>
      prev.includes(id) ? prev.filter((m) => m !== id) : [...prev, id],
    );
  };

  const uniqueRoles = useMemo(
    () => [...new Set(members?.map((m) => m.role))],
    [members],
  );

  const filteredMembers = useMemo(() => {
    return members?.filter((member) => {
      const matchesSearch =
        member.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        member.email.toLowerCase().includes(searchQuery.toLowerCase()) ||
        member.role.toLowerCase().includes(searchQuery.toLowerCase());

      const matchesRole =
        selectedRoles.length === 0 || selectedRoles.includes(member.role);

      return matchesSearch && matchesRole;
    });
  }, [members, searchQuery, selectedRoles]);

  const handleRoleToggle = (role: string) => {
    setSelectedRoles((prev) =>
      prev.includes(role) ? prev.filter((r) => r !== role) : [...prev, role],
    );
  };

  const hasFilters = selectedRoles.length > 0 || searchQuery !== "";

  return (
    <div className="h-full flex flex-col bg-background relative overflow-hidden">
      {/* Header with Controls */}
      <div className="border-b border-border px-2 py-1 flex items-center justify-between gap-2">
        {/* Search */}
        <div className="relative flex-1 max-w-md">
          <Search className="absolute left-2 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-muted-foreground/40" />
          <Input
            placeholder="Search members..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="pl-7 h-7 text-[10px] rounded-sm bg-muted/20 border-border/10 focus:bg-muted/40 transition-colors placeholder:text-muted-foreground/30"
          />
        </div>

        {/* Actions */}
        <div className="flex items-center gap-1.5">
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                variant={hasFilters ? "default" : "outline"}
                size="sm"
                className="h-7 gap-1 rounded-sm text-[10px] font-bold uppercase tracking-tight px-2"
              >
                <Filter className="h-3.5 w-3.5" />
                Filter
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-48 rounded-sm">
              <div className="px-2 py-1.5">
                <p className="text-[10px] font-black uppercase tracking-wider text-muted-foreground mb-2">
                  Role
                </p>
                {uniqueRoles.map((role) => (
                  <DropdownMenuCheckboxItem
                    key={role}
                    checked={selectedRoles.includes(role)}
                    onCheckedChange={() => handleRoleToggle(role)}
                    className="text-[11px]"
                  >
                    {role}
                  </DropdownMenuCheckboxItem>
                ))}
              </div>

              {hasFilters && (
                <>
                  <DropdownMenuSeparator />
                  <button
                    onClick={() => {
                      setSearchQuery("");
                      setSelectedRoles([]);
                    }}
                    className="w-full text-left px-2 py-1.5 text-[10px] font-bold text-muted-foreground hover:text-foreground transition-colors uppercase tracking-tight"
                  >
                    Clear filters
                  </button>
                </>
              )}
            </DropdownMenuContent>
          </DropdownMenu>

          <Button
            onClick={onAddMember}
            size="sm"
            className="h-7 gap-1 rounded-sm text-[10px] font-bold uppercase tracking-tight px-2"
          >
            <Plus className="h-3.5 w-3.5" />
            Add Member
          </Button>
        </div>
      </div>

      {/* Members Table */}
      <div className="flex-1 overflow-auto">
        {filteredMembers?.length === 0 ? (
          <div className="h-full flex items-center justify-center">
            <div className="text-center space-y-3">
              <p className="text-[11px] text-muted-foreground">
                No members found
              </p>
              {hasFilters && (
                <Button
                  variant="outline"
                  size="sm"
                  className="h-7 rounded-sm text-[10px] font-bold uppercase tracking-tight"
                  onClick={() => {
                    setSearchQuery("");
                    setSelectedRoles([]);
                  }}
                >
                  Clear filters
                </Button>
              )}
            </div>
          </div>
        ) : (
          <div className="px-1 py-2">
            {/* Table Header */}
            <div className="grid grid-cols-[45px_3fr_1fr_1fr_80px_55px] gap-0 text-[9px] font-black uppercase tracking-wider text-muted-foreground/50 border-b border-border/40 items-center bg-muted/20">
              <div className="flex items-center justify-center py-0.5 border-r border-border/10">
                <Checkbox
                 className="rounded-sm"
                  checked={
                    selectedMembers.length === filteredMembers?.length &&
                    filteredMembers?.length > 0
                  }
                  onCheckedChange={() => {
                    if (selectedMembers.length === filteredMembers?.length) {
                      setSelectedMembers([]);
                    } else {
                      setSelectedMembers(
                        filteredMembers?.map((m) => m.id) || [],
                      );
                    }
                  }}
                />
              </div>
              <div className="px-2 py-0.5 border-r border-border/10">
                Member
              </div>
              <div className="px-2 py-0.5 border-r border-border/10">Role</div>
              <div className="px-2 py-0.5 border-r border-border/10">Status</div>
              <div className="px-2 py-0.5 border-r border-border/10">Joined</div>
              <div className="text-right px-2 py-0.5">Actions</div>
            </div>

            {/* Table Body */}
            <div className="divide-y divide-border/30 border-b border-border/30">
              {filteredMembers?.map((member) => (
                <div
                  key={member.id}
                  className="grid grid-cols-[45px_3fr_1fr_1fr_80px_55px] gap-0 text-[11px] items-center hover:bg-muted/5 transition-colors"
                >
                  {/* Checkbox Column */}
                  <div className="flex items-center justify-center py-2 border-r border-border/10 h-full">
                    <Checkbox
                      className="rounded-sm"
                      checked={selectedMembers.includes(member.id)}
                      onCheckedChange={() => toggleMemberSelection(member.id)}
                    />
                  </div>

                  {/* Member Column */}
                  <div className="flex items-center gap-3 min-w-0 px-3 py-2 border-r border-border/10 h-full">
                    <Avatar className="h-6 w-6 rounded-sm flex-shrink-0">
                      <AvatarImage src={member.avatarUrl || ""} />
                      <AvatarFallback className="text-[10px] bg-muted rounded-sm">
                        {member.name.substring(0, 1)}
                      </AvatarFallback>
                    </Avatar>
                    <div className="flex flex-col min-w-0">
                      <span className="font-medium text-foreground truncate">
                        {member.name}
                      </span>
                      <span className="text-muted-foreground/60 text-[10px] truncate">
                        {member.email}
                      </span>
                    </div>
                  </div>

                  {/* Role Column */}
                  <div className="px-3 py-2 border-r border-border/10 h-full flex items-center">
                    <RoleBadge role={member.role as any} />
                  </div>

                  {/* Status Column */}
                  <div className="px-3 py-2 border-r border-border/10 h-full flex items-center">
                    <span className="text-muted-foreground/60 text-[10px] uppercase font-bold tracking-tight">
                      {member.status || "Active"}
                    </span>
                  </div>

                  {/* Joined Column */}
                  <div className="px-3 py-2 border-r border-border/10 h-full flex items-center">
                    <span className="text-muted-foreground/60 text-[10px]">
                      {member.joinedAt ? new Date(member.joinedAt).toLocaleDateString() : "Invited"}
                    </span>
                  </div>

                  {/* Actions Column */}
                  <div className="text-right px-3 py-2 h-full flex items-center justify-end">
                    <DropdownMenu>
                      <DropdownMenuTrigger asChild>
                        <Button
                          variant="ghost"
                          size="sm"
                          className="h-6 w-6 p-0 hover:bg-muted rounded-sm"
                        >
                          <MoreVertical className="h-3.5 w-3.5 text-muted-foreground/40" />
                        </Button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent
                        align="end"
                        className="rounded-sm w-36"
                      >
                        <DropdownMenuItem className="text-[11px] py-1">
                          Tasks
                        </DropdownMenuItem>
                        <DropdownMenuItem className="text-[11px] py-1">
                          Suspend
                        </DropdownMenuItem>
                        <DropdownMenuItem
                          onClick={() => onDeleteMember?.(member.id)}
                          className="text-[11px] py-1 text-destructive focus:text-destructive focus:bg-destructive/10"
                        >
                          Remove
                        </DropdownMenuItem>
                      </DropdownMenuContent>
                    </DropdownMenu>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>

      {/* Floating Action Bar */}
      {selectedMembers.length > 0 && (
        <div className="absolute bottom-6 left-1/2 -translate-x-1/2 z-50 animate-in fade-in slide-in-from-bottom-5 duration-300">
          <div className="flex items-center gap-2 px-2.5 py-1.5 rounded-md bg-card/90 backdrop-blur-xl border border-border shadow-lg">
            <div className="flex items-center gap-1.5 px-2 py-1 rounded-sm bg-primary/10 border border-primary/20 h-7">
              <span className="text-[11px] font-bold font-mono text-primary">
                {selectedMembers.length}
              </span>
              <span className="text-[10px] font-black font-mono text-primary uppercase tracking-wider">
                Selected
              </span>
            </div>

            <div className="h-6 w-px bg-border/50" />

            <div className="flex items-center gap-1.5">
              <Select
                value={selectedRole}
                onValueChange={(v) => setSelectedRole(v as Role)}
              >
                <SelectTrigger className="w-24 h-7 px-2 rounded-sm bg-background/50 border-border/50 text-[10px] font-medium hover:bg-accent transition-colors">
                  <SelectValue placeholder="Role" />
                </SelectTrigger>
                <SelectContent className="rounded-sm border-border/50 bg-card/95 backdrop-blur-xl">
                  <SelectItem
                    value="Admin"
                    className="text-[10px] tracking-tight py-1"
                  >
                    Admin
                  </SelectItem>
                  <SelectItem
                    value="Member"
                    className="text-[10px] tracking-tight py-1"
                  >
                    Member
                  </SelectItem>
                  <SelectItem
                    value="Guest"
                    className="text-[10px] tracking-tight py-1"
                  >
                    Guest
                  </SelectItem>
                </SelectContent>
              </Select>

              <Select
                value={selectedStatus}
                onValueChange={(v) => setSelectedStatus(v as MembershipStatus)}
              >
                <SelectTrigger className="w-24 h-7 px-2 rounded-sm bg-background/50 border-border/50 text-[10px] font-medium hover:bg-accent transition-colors">
                  <SelectValue placeholder="Status" />
                </SelectTrigger>
                <SelectContent className="rounded-sm border-border/50 bg-card/95 backdrop-blur-xl">
                  <SelectItem
                    value="Active"
                    className="text-[10px] tracking-tight py-1"
                  >
                    Active
                  </SelectItem>
                  <SelectItem
                    value="Suspended"
                    className="text-[10px] tracking-tight py-1"
                  >
                    Suspend
                  </SelectItem>
                </SelectContent>
              </Select>

              {(selectedRole || selectedStatus) && (
                <Button
                  size="sm"
                  className="h-7 px-3 rounded-sm bg-primary text-primary-foreground hover:bg-primary/90 transition-all font-bold text-[10px] uppercase tracking-tight gap-1"
                  onClick={() => {
                    onBatchUpdate?.(
                      selectedMembers,
                      selectedRole || undefined,
                      selectedStatus || undefined,
                    );
                    setSelectedMembers([]);
                    setSelectedRole("");
                    setSelectedStatus("");
                  }}
                >
                  <Check className="h-3 w-3" />
                  Apply
                </Button>
              )}

              <Button
                size="sm"
                variant="outline"
                className="h-7 w-7 p-0 rounded-sm bg-background/50 border-border/50 hover:bg-destructive/10 hover:text-destructive hover:border-destructive/30 transition-all"
                onClick={() => {
                  onBatchDelete?.(selectedMembers);
                  setSelectedMembers([]);
                }}
                title="Remove selected"
              >
                <Trash2 className="h-4 w-4" />
              </Button>

              <div className="h-6 w-px bg-border/50" />

              <Button
                size="sm"
                variant="ghost"
                className="h-7 w-7 p-0 rounded-sm hover:bg-muted transition-colors"
                onClick={() => {
                  setSelectedMembers([]);
                  setSelectedRole("");
                  setSelectedStatus("");
                }}
              >
                <X className="h-4 w-4 text-muted-foreground/40" />
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
