import { useMemo, useState } from "react";
import type { MemberSummary } from "../members-type";
import { Button } from "@/components/ui/button";
import { Check, Edit2, Filter, Plus, Search, Trash2, X } from "lucide-react";
import { Input } from "@/components/ui/input";
import {
  DropdownMenu,
  DropdownMenuCheckboxItem,
  DropdownMenuContent,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { MemberCard } from "./member-card";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import type { Role } from "@/types/role";
import type { MembershipStatus } from "@/types/membership-status";

interface Props {
  members?: MemberSummary[];
  onAddMember?: () => void;
  onEditMember?: (id: string, role: string) => void;
  onDeleteMember?: (id: string) => void;
  onBatchUpdate?: (
    ids: string[],
    role?: Role,
    status?: MembershipStatus,
  ) => void;
  onBatchDelete?: (ids: string[]) => void;
}

export function MemberGridList({
  members,
  onAddMember,
  onBatchUpdate,
  onBatchDelete,
  onDeleteMember,
}: Props) {
  const [searchQuery, setSearchQuery] = useState("");
  const [selectedRoles, setSelectedRoles] = useState<string[]>([]);
  const [isEditMode, setIsEditMode] = useState(false);
  const [selectedMembers, setSelectedMembers] = useState<string[]>([]);
  const [selectedRole, setSelectedRole] = useState<Role | "">("");
  const [selectedStatus, setSelectedStatus] = useState<MembershipStatus | "">(
    "",
  );

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

  const toggleMemberSelection = (id: string) => {
    setSelectedMembers((prev) =>
      prev.includes(id) ? prev.filter((m) => m !== id) : [...prev, id],
    );
  };

  const handleExitEditMode = () => {
    setIsEditMode(false);
    setSelectedMembers([]);
    setSelectedRole("");
    setSelectedStatus("");
  };

  const hasFilters = selectedRoles.length > 0 || searchQuery !== "";

  const handleApplyBatchUpdate = () => {
    if (selectedMembers.length > 0 && (selectedRole || selectedStatus)) {
      onBatchUpdate?.(
        selectedMembers,
        selectedRole || undefined,
        selectedStatus || undefined,
      );
    }
  };

  return (
    <div className="h-full flex flex-col bg-background relative overflow-hidden">
      {/* Header with Controls */}
      <div className="border-b border-border p-6 space-y-4">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-foreground">Members</h1>
            <p className="text-sm text-muted-foreground mt-1">
              Manage team members and their roles
            </p>
          </div>
          <Button onClick={onAddMember} size="lg" className="gap-2 rounded-lg">
            <Plus className="h-4 w-4" />
            Add Member
          </Button>
        </div>

        {/* Search and Filters */}
        <div className="flex gap-3">
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
            <Input
              placeholder="Search by name, email, or role..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="pl-10 h-10 rounded-lg"
            />
          </div>

          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                variant={hasFilters ? "default" : "outline"}
                size="lg"
                className="gap-2 rounded-lg"
              >
                <Filter className="h-4 w-4" />
                Filter
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-56">
              <div className="px-2 py-1.5">
                <p className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-2">
                  Role
                </p>
                {uniqueRoles.map((role) => (
                  <DropdownMenuCheckboxItem
                    key={role}
                    checked={selectedRoles.includes(role)}
                    onCheckedChange={() => handleRoleToggle(role)}
                    className="text-sm"
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
                    className="w-full text-left px-2 py-1.5 text-xs text-muted-foreground hover:text-foreground transition-colors"
                  >
                    Clear filters
                  </button>
                </>
              )}
            </DropdownMenuContent>
          </DropdownMenu>

          <Button
            variant={isEditMode ? "default" : "outline"}
            size="lg"
            onClick={() =>
              isEditMode ? handleExitEditMode() : setIsEditMode(true)
            }
            className="gap-2 rounded-lg"
          >
            <Edit2 className="h-4 w-4" />
            {isEditMode ? "Done" : "Edit"}
          </Button>
        </div>

        {hasFilters && (
          <p className="text-xs text-muted-foreground">
            Showing {filteredMembers?.length} of {members?.length} members
          </p>
        )}
      </div>

      {/* Members Grid/List */}
      <div className="flex-1 overflow-auto pb-32">
        {filteredMembers?.length === 0 ? (
          <div className="h-full flex items-center justify-center">
            <div className="text-center space-y-3">
              <p className="text-muted-foreground">No members found</p>
              {hasFilters && (
                <Button
                  variant="outline"
                  size="sm"
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
          <div className="p-6 grid gap-4 grid-cols-1 sm:grid-cols-2 lg:grid-cols-3">
            {filteredMembers?.map((member) => (
              <MemberCard
                member={member}
                key={member.id}
                isEditMode={isEditMode}
                isSelected={selectedMembers.includes(member.id)}
                onSelect={() => toggleMemberSelection(member.id)}
                onDelete={onDeleteMember || (() => {})}
              />
            ))}
          </div>
        )}
      </div>

      {/* Floating Action Bar */}
      {isEditMode && selectedMembers.length > 0 && (
        <div className="fixed bottom-10 left-1/2 -translate-x-1/2 z-50 animate-in fade-in slide-in-from-bottom-5 duration-300">
          <div className="flex items-center gap-3 px-4 py-3 rounded-2xl bg-card/80 backdrop-blur-xl border border-border shadow-2xl ring-1 ring-black/5">
            <div className="flex items-center gap-2 px-3 py-1.5 rounded-xl bg-primary/10 border border-primary/20">
              <span className="text-xs font-bold font-mono text-primary">
                {selectedMembers.length}
              </span>
              <span className="text-[10px] font-bold font-mono text-primary uppercase tracking-wider">
                Selected
              </span>
            </div>

            <div className="h-8 w-px bg-border/50 mx-1" />

            <div className="flex items-center gap-2">
              <Select
                value={selectedRole}
                onValueChange={(v) => setSelectedRole(v as Role)}
              >
                <SelectTrigger className="w-28 h-9 rounded-xl bg-background/50 border-border/50 text-xs font-medium hover:bg-accent transition-colors">
                  <SelectValue placeholder="Role" />
                </SelectTrigger>
                <SelectContent className="rounded-xl border-border/50 bg-card/95 backdrop-blur-xl">
                  <SelectItem value="Admin" className="text-xs tracking-tight">
                    Admin
                  </SelectItem>
                  <SelectItem value="Member" className="text-xs tracking-tight">
                    Member
                  </SelectItem>
                  <SelectItem value="Guest" className="text-xs tracking-tight">
                    Guest
                  </SelectItem>
                </SelectContent>
              </Select>

              <Select
                value={selectedStatus}
                onValueChange={(v) => setSelectedStatus(v as MembershipStatus)}
              >
                <SelectTrigger className="w-28 h-9 rounded-xl bg-background/50 border-border/50 text-xs font-medium hover:bg-accent transition-colors">
                  <SelectValue placeholder="Status" />
                </SelectTrigger>
                <SelectContent className="rounded-xl border-border/50 bg-card/95 backdrop-blur-xl">
                  <SelectItem value="Active" className="text-xs tracking-tight">
                    Active
                  </SelectItem>
                  <SelectItem
                    value="Suspended"
                    className="text-xs tracking-tight"
                  >
                    Suspended
                  </SelectItem>
                </SelectContent>
              </Select>

              {(selectedRole || selectedStatus) && (
                <Button
                  size="sm"
                  className="h-9 px-4 rounded-xl bg-primary text-primary-foreground hover:bg-primary/90 transition-all font-medium text-xs gap-2"
                  onClick={handleApplyBatchUpdate}
                >
                  <Check className="h-3.5 w-3.5" />
                  Apply
                </Button>
              )}

              <Button
                size="sm"
                variant="outline"
                className="h-9 w-9 p-0 rounded-xl bg-background/50 border-border/50 hover:bg-destructive/10 hover:text-destructive hover:border-destructive/30 transition-all border-dashed"
                onClick={() => onBatchDelete?.(selectedMembers)}
                title="Remove selected"
              >
                <Trash2 className="h-4 w-4 transition-transform hover:scale-110" />
              </Button>

              <div className="h-8 w-px bg-border/50 mx-1" />

              <Button
                size="sm"
                variant="ghost"
                className="h-9 w-9 p-0 rounded-xl hover:bg-accent transition-colors"
                onClick={handleExitEditMode}
              >
                <X className="h-4 w-4" />
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
