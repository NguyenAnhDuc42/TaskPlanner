import { useMemo, useState } from "react"
import type { MemberSummary } from "../members-type"
import { Button } from "@/components/ui/button"
import { Ban, Edit2, Filter, Plus, Search, Shield, Trash2, X } from "lucide-react"
import { Input } from "@/components/ui/input"
import { DropdownMenu, DropdownMenuCheckboxItem, DropdownMenuContent, DropdownMenuSeparator, DropdownMenuTrigger } from "@/components/ui/dropdown-menu"
import { MemberCard } from "./member-card"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"

interface Props {
  members?: MemberSummary[]
  onAddMember?: () => void
  onEditMember?: (id: string, role: string) => void
  onDeleteMember?: (id: string) => void
}

export function MemberGridList ({ members, onAddMember, onEditMember, onDeleteMember }: Props) {
    const [searchQuery, setSearchQuery] = useState("")
  const [selectedRoles, setSelectedRoles] = useState<string[]>([])
  const [selectedStatus, setSelectedStatus] = useState<string[]>([])
  const [isEditMode, setIsEditMode] = useState(false)
  const [selectedMembers, setSelectedMembers] = useState<string[]>([])
  const [editingMemberId, setEditingMemberId] = useState<string | null>(null)
  const [editingMemberRole, setEditingMemberRole] = useState<string>("")
  const [selectedRole, setSelectedRole] = useState<string>("")

  // Get unique roles and statuses
  const uniqueRoles = useMemo(() => [...new Set(members?.map((m) => m.role))], [members])

  // Filter members based on search and filters
  const filteredMembers = useMemo(() => {
    return members?.filter((member) => {
      const matchesSearch =
        member.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        member.email.toLowerCase().includes(searchQuery.toLowerCase()) ||
        member.role.toLowerCase().includes(searchQuery.toLowerCase())

      const matchesRole = selectedRoles.length === 0 || selectedRoles.includes(member.role)

      return matchesSearch && matchesRole
    })
  }, [members, searchQuery, selectedRoles])

  const handleRoleToggle = (role: string) => {
    setSelectedRoles((prev) =>
      prev.includes(role) ? prev.filter((r) => r !== role) : [...prev, role]
    )
  }

  const toggleMemberSelection = (id: string) => {
    setSelectedMembers((prev) =>
      prev.includes(id) ? prev.filter((m) => m !== id) : [...prev, id]
    )
  }

  const handleExitEditMode = () => {
    setIsEditMode(false)
    setSelectedMembers([])
    setEditingMemberRole("")
  }

  const handleChangeRole = () => {
    selectedMembers.forEach((id) => {
      console.log(`[v0] Changing member ${id} role to ${editingMemberRole}`)
      onEditMember?.(id, editingMemberRole)
    })
    handleExitEditMode()
  }

  const handleMuteMembers = () => {
    selectedMembers.forEach((id) => {
      console.log(`[v0] Muting member ${id}`)
    })
    handleExitEditMode()
  }

  const handleDeleteSelected = () => {
    selectedMembers.forEach((id) => {
      onDeleteMember?.(id)
    })
    handleExitEditMode()
  }

  const hasFilters = selectedRoles.length > 0 || selectedStatus.length > 0 || searchQuery !== ""

  const handleEditMember = (id: string) => {
    setEditingMemberId(id)
    const member = members?.find((m) => m.id === id)
    if (member) {
      setEditingMemberRole(member.role)
    }
  }

  const getEditingMember = () => {
    return members?.find((m) => m.id === editingMemberId)
  }

  return (
    <div className="h-full flex flex-col bg-background">
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
          {/* Search Input */}
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
            <Input
              placeholder="Search by name, email, or role..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="pl-10 h-10 rounded-lg"
            />
          </div>

          {/* Filter Dropdown */}
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
              {/* Role Filter */}
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

              <DropdownMenuSeparator />

              {/* Clear Filters */}
              {hasFilters && (
                <>
                  <DropdownMenuSeparator />
                  <button
                    onClick={() => {
                      setSearchQuery("")
                      setSelectedRoles([])
                      setSelectedStatus([])
                    }}
                    className="w-full text-left px-2 py-1.5 text-xs text-muted-foreground hover:text-foreground transition-colors"
                  >
                    Clear filters
                  </button>
                </>
              )}
            </DropdownMenuContent>
          </DropdownMenu>

          {/* Edit Button */}
          <Button
            variant={isEditMode ? "default" : "outline"}
            size="sm"
            onClick={() => (isEditMode ? handleExitEditMode() : setIsEditMode(true))}
            className="gap-2"
          >
            <Edit2 className="h-4 w-4" />
            {isEditMode ? "Done" : "Edit"}
          </Button>
        </div>

        {/* Results Count */}
        {hasFilters && (
          <p className="text-xs text-muted-foreground">
            Showing {filteredMembers?.length} of {members?.length} members
          </p>
        )}
      </div>

      {/* Members Grid/List */}
      <div className="flex-1 overflow-auto">
        {filteredMembers?.length === 0 ? (
          <div className="h-full flex items-center justify-center">
            <div className="text-center space-y-3">
              <p className="text-muted-foreground">No members found</p>
              {hasFilters && (
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => {
                    setSearchQuery("")
                    setSelectedRoles([])
                    setSelectedStatus([])
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
                    member={member} key={member.id}
                    isEditMode={isEditMode}
                    isSelected={selectedMembers.includes(member.id)}
                    onSelect={() => toggleMemberSelection(member.id)}
                    onDelete={onDeleteMember || (() => { })}              />
            ))}
          </div>
        )}
      </div>

      {/* Floating Action Bar */}
      {isEditMode && selectedMembers.length > 0 && (
        <div className="fixed bottom-0 left-0 right-0 bg-card border-t border-border shadow-2xl z-50">
          <div className="max-w-6xl mx-auto px-6 py-4 flex items-center justify-between">
            <div className="flex items-center gap-4">
              <span className="text-sm font-medium text-foreground">
                {selectedMembers.length} member{selectedMembers.length !== 1 ? "s" : ""} selected
              </span>
            </div>

            <div className="flex items-center gap-3">
              {/* Change Role */}
              <Select value={selectedRole} onValueChange={setSelectedRole}>
                <SelectTrigger className="w-40 rounded-lg h-9">
                  <SelectValue placeholder="Change role..." />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Team Lead">Team Lead</SelectItem>
                  <SelectItem value="Developer">Developer</SelectItem>
                  <SelectItem value="Designer">Designer</SelectItem>
                  <SelectItem value="Product Manager">Product Manager</SelectItem>
                  <SelectItem value="QA Engineer">QA Engineer</SelectItem>
                </SelectContent>
              </Select>

              {selectedRole && (
                <Button
                  size="sm"
                  className="rounded-lg h-9 gap-2"
                  onClick={handleChangeRole}
                >
                  <Shield className="h-4 w-4" />
                  Apply Role
                </Button>
              )}

              {/* Mute Button */}
              <Button
                size="sm"
                variant="outline"
                className="rounded-lg h-9 gap-2 bg-transparent"
                onClick={handleMuteMembers}
              >
                <Ban className="h-4 w-4" />
                Mute
              </Button>

              {/* Delete Button */}
              <Button
                size="sm"
                variant="ghost"
                className="rounded-lg h-9 gap-2 text-destructive hover:text-destructive hover:bg-destructive/10"
                onClick={handleDeleteSelected}
              >
                <Trash2 className="h-4 w-4" />
                Delete
              </Button>

              {/* Close */}
              <Button
                size="sm"
                variant="ghost"
                className="rounded-lg h-9 w-9 p-0"
                onClick={handleExitEditMode}
              >
                <X className="h-4 w-4" />
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
