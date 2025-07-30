"use client"

import { useState } from "react"
import { use } from "react"
import { MembersHeader } from "@/app/(main)/ws/[workspaceId]/members/component/members-header"
import { MembersFilter } from "@/app/(main)/ws/[workspaceId]/members/component/members-filter"
import { MembersTable } from "@/app/(main)/ws/[workspaceId]/members/component/members-table"
import { InviteMembersDialog } from "@/app/(main)/ws/[workspaceId]/members/component/invite-members-dialog"
import { useAddMembers, useGetMembers, useUpdateMembers } from "@/features/workspace/workspace-hooks"
import type { AddMembersBody, UpdateMembersBody } from "@/features/workspace/workspacetype"
import type { Role } from "@/utils/role-utils"
import { UpdateRolesDialog } from "./component/update-roles-dialog"

export default function MembersPage({ params }: { params: Promise<{ workspaceId: string }> }) {
  const resolvedParams = use(params)
  const workspaceId = resolvedParams.workspaceId

  const [searchQuery, setSearchQuery] = useState("")
  const [selectedFilter, setSelectedFilter] = useState("all")
  const [inviteDialogOpen, setInviteDialogOpen] = useState(false)
  const [updateRolesDialogOpen, setUpdateRolesDialogOpen] = useState(false)
  const [selectedMemberIds, setSelectedMemberIds] = useState<string[]>([])

  // Use your provided hooks
  const { data: membersData, isLoading } = useGetMembers(workspaceId)
  const addMembersMutation = useAddMembers(workspaceId)
  const updateMembersMutation = useUpdateMembers(workspaceId)

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
    const updateData: UpdateMembersBody = {
      memberIds: selectedMemberIds,
      role: newRole,
    }

    try {
      await updateMembersMutation.mutateAsync(updateData)
      setSelectedMemberIds([]) // Clear selection after successful update
    } catch (error) {
      console.error("Failed to update member roles:", error)
      throw error
    }
  }

  const handleInviteMembers = async (data: AddMembersBody) => {
    try {
      await addMembersMutation.mutateAsync(data)
    } catch (error) {
      console.error("Failed to invite members:", error)
      throw error
    }
  }

  const handleExport = () => {
    console.log("Exporting members data")
  }

  if (!workspaceId) {
    return <div className="text-white">Workspace ID is required</div>
  }

  return (
    <div className="min-h-screen bg-gray-900 p-6">
      <div className="max-w-7xl mx-auto space-y-6">
        <MembersHeader
          searchQuery={searchQuery}
          onSearchChange={setSearchQuery}
          onInviteClick={() => setInviteDialogOpen(true)}
          onExportClick={handleExport}
        />

        <MembersFilter
          totalCount={filteredMembers.length}
          selectedFilter={selectedFilter}
          onFilterChange={setSelectedFilter}
          onInviteClick={() => setInviteDialogOpen(true)}
        />

        {isLoading ? (
          <div className="text-white">Loading members...</div>
        ) : (
          <MembersTable
            members={filteredMembers}
            selectedMembers={selectedMemberIds}
            onSelectionChange={handleMemberSelection}
            onSelectAll={handleSelectAll}
            onUpdateRoles={handleUpdateRoles}
          />
        )}

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
      </div>
    </div>
  )
}
