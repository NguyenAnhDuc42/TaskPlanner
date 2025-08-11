"use client"

import { useState } from "react"
import { toast } from "sonner"
import { MembersHeader } from "@/app/(main)/ws/[workspaceId]/members/(component)/members-header"
import { MembersFilter } from "@/app/(main)/ws/[workspaceId]/members/(component)/members-filter"
import { MembersTable } from "@/app/(main)/ws/[workspaceId]/members/(component)/members-table"
import { InviteMembersDialog } from "@/app/(main)/ws/[workspaceId]/members/(component)/invite-members-dialog"
import { useAddMembers, useDeleteMembers, useGetMembers, useUpdateMembers } from "@/features/workspace/workspace-hooks"
import type { Role } from "@/utils/role-utils"
import { UpdateRolesDialog } from "./(component)/update-roles-dialog"
import { useWorkspaceId } from "@/utils/currrent-layer-id"
import { MembersTableSkeleton } from "./(component)/members-table-skeleton"
import { MembersEmptyState } from "./(component)/members-empty-state"
import { DeleteMembersDialog } from "./(component)/delete-members-dialog"

export default function MembersPage() {

  const workspaceId = useWorkspaceId();

  const [searchQuery, setSearchQuery] = useState("")
  const [selectedFilter, setSelectedFilter] = useState("all")
  const [inviteDialogOpen, setInviteDialogOpen] = useState(false)
  const [updateRolesDialogOpen, setUpdateRolesDialogOpen] = useState(false)
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false)
  const [selectedMemberIds, setSelectedMemberIds] = useState<string[]>([])

  // Use your provided hooks
  const { data: membersData, isLoading, isError, error: membersError } = useGetMembers(workspaceId)
  const addMembersMutation = useAddMembers(workspaceId)
  const updateMembersMutation = useUpdateMembers(workspaceId)
  const deleteMembersMutation = useDeleteMembers(workspaceId)

  const members = membersData || []

  const filteredMembers = members.filter((member) => {
    const matchesSearch =
      member.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
      member.email.toLowerCase().includes(searchQuery.toLowerCase())

    const matchesFilter = selectedFilter === "all" || member.role === selectedFilter
    return matchesSearch && matchesFilter
  })

  const selectedMembers = members.filter((member) => selectedMemberIds.includes(member.id))

  const handleMemberSelection = (memberId: string, selected: boolean) => {
    setSelectedMemberIds((prev) => (selected ? [...prev, memberId] : prev.filter((id) => id !== memberId)))
  }

  const handleSelectAll = (selected: boolean) => {
    setSelectedMemberIds(selected ? filteredMembers.map((member) => member.id) : [])
  }

  const handleUpdateRoles = () => {
    setUpdateRolesDialogOpen(true)
  }

  const handleRoleUpdate = async (newRole: Role) => {
    const promise = updateMembersMutation.mutateAsync({
      memberIds: selectedMemberIds,
      role: newRole,
    })

    toast.promise(promise, {
      loading: "Updating roles...",
      success: () => {
        setSelectedMemberIds([]) // Clear selection on success
        return "Member roles updated successfully."
      },
      error: "Failed to update roles. Please try again.",
    })
  }

  const handleInviteMembers = async (data: { emails: string[]; role: Role }) => {
    const promise = addMembersMutation.mutateAsync(data)

    toast.promise(promise, {
      loading: "Sending invitations...",
      success: "Invitations sent successfully.",
      error: "Failed to send invitations. Please try again.",
    })
  }

  const handleDeleteMembers = () => {
    const promise = deleteMembersMutation.mutateAsync(selectedMemberIds)

    toast.promise(promise, {
      loading: "Removing members...",
      success: () => {
        setSelectedMemberIds([]) // Clear selection on success
        setDeleteDialogOpen(false) // Close dialog on success
        return "Members removed successfully."
      },
      error: (err) => {
        // Assuming err has a 'detail' property from your ErrorResponse type
        return err.detail || "Failed to remove members. Please try again."
      },
    })
  }

  const handleExport = () => {
    console.log("Exporting members data")
    toast.info("Export functionality is not yet implemented.")
  }

  const renderContent = () => {
    if (isLoading) {
      return <MembersTableSkeleton />
    }

    if (isError) {
      return (
        <MembersEmptyState
          title="Error Loading Members"
          description={membersError?.detail || "Could not fetch members data. Please try again later."}
        />
      )
    }

    if (members.length === 0) {
      return (
        <MembersEmptyState
          title="Invite people to your workspace"
          description="Collaborate with your team by inviting them to this workspace."
          actionText="Invite People"
          onActionClick={() => setInviteDialogOpen(true)}
        />
      )
    }

    if (filteredMembers.length === 0) {
      return (
        <MembersEmptyState
          title="No members found"
          description="Try adjusting your search or filter to find what you're looking for."
        />
      )
    }

    return (
      <MembersTable
        members={filteredMembers}
        selectedMembers={selectedMemberIds}
        onSelectionChange={handleMemberSelection}
        onSelectAll={handleSelectAll}
        onUpdateRoles={handleUpdateRoles}
        onDeleteMembers={() => setDeleteDialogOpen(true)}
      />
    )
  }

  return (
    <div className="p-6 h-full flex flex-col max-w-7xl mx-auto w-full">
      {/* Non-scrolling Header and Filter Section */}
      <div className="shrink-0 space-y-3">
        <MembersHeader searchQuery={searchQuery} onSearchChange={setSearchQuery} onInviteClick={() => setInviteDialogOpen(true)} onExportClick={handleExport} />

        <MembersFilter
          filteredCount={filteredMembers.length}
          selectedFilter={selectedFilter}
          totalCount={members.length}
          onFilterChange={setSelectedFilter}
          // This prop is no longer needed on MembersFilter but keeping it doesn't harm anything
          onInviteClick={() => setInviteDialogOpen(true)}
        />
      </div>

      {/* Scrollable Content Section */}
      <div className="flex-1 overflow-y-auto mt-4">
        <div className="pr-2">{renderContent()}</div>
      </div>

      {/* Dialogs remain outside the scrollable area */}
      <div className="shrink-0">
        <InviteMembersDialog
          open={inviteDialogOpen}
          onOpenChange={setInviteDialogOpen}
          onInvite={handleInviteMembers}
          isLoading={addMembersMutation.isPending}
        />

        <UpdateRolesDialog
          open={updateRolesDialogOpen}
          onOpenChange={setUpdateRolesDialogOpen}
          selectedMembers={selectedMembers}
          onUpdateRoles={handleRoleUpdate}
          isLoading={updateMembersMutation.isPending}
        />

        <DeleteMembersDialog
          open={deleteDialogOpen}
          onOpenChange={setDeleteDialogOpen}
          selectedMemberCount={selectedMemberIds.length}
          onConfirm={handleDeleteMembers}
          isLoading={deleteMembersMutation.isPending}
        />
      </div>
    </div>
  )
}
