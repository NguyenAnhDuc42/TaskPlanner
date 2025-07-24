"use client"

import { useState } from "react"
import { use } from "react"
import { MembersHeader } from "@/components/members/members-header"
import { MembersFilter } from "@/components/members/members-filter"
import { MembersTable } from "@/components/members/members-table"
import { InviteMembersDialog } from "@/components/members/invite-members-dialog"
import type { Member } from "@/types/user" // Import Member from types/user
import { useAddMembers, useGetMembers } from "@/features/workspace/workspace-hooks"

export default function MembersPage({ params }: { params: Promise<{ workspaceId: string }> }) {
  const resolvedParams = use(params)
  const workspaceId = resolvedParams.workspaceId

  const [searchQuery, setSearchQuery] = useState("")
  const [selectedFilter, setSelectedFilter] = useState("all")
  const [inviteDialogOpen, setInviteDialogOpen] = useState(false)

  // Use your provided hook directly
  const { data: membersData, isLoading } = useGetMembers(workspaceId)
  const addMembersMutation = useAddMembers(workspaceId)

  // Use members from the hook, default to an empty array if not loaded yet
  const members: Member[] = membersData?.members || []

  const filteredMembers = members.filter((member) => {
    const matchesSearch =
      member.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
      member.email.toLowerCase().includes(searchQuery.toLowerCase())

    // Filter directly by the string role
    const matchesFilter = selectedFilter === "all" || member.role === selectedFilter
    return matchesSearch && matchesFilter
  })

  const handleInviteMembers = async (emails: string[]) => {
    try {
      await addMembersMutation.mutateAsync(emails)
      setInviteDialogOpen(false)
    } catch (error) {
      console.error("Failed to invite members:", error)
    }
  }

  const handleExport = () => {
    // Implement export logic
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

        {isLoading ? <div className="text-white">Loading members...</div> : <MembersTable members={filteredMembers} />}

        <InviteMembersDialog
          open={inviteDialogOpen}
          onOpenChange={setInviteDialogOpen}
          onInvite={handleInviteMembers}
          isLoading={addMembersMutation.isPending}
        />
      </div>
    </div>
  )
}
