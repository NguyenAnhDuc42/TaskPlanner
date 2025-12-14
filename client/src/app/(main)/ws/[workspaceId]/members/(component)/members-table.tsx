"use client"

import type { UserSummary } from "@/types/user"
import { MemberRow } from "./member-row"
import { Button } from "@/components/ui/button"
import { Users } from "lucide-react"

interface MembersTableProps {
  members: UserSummary[]
  selectedMembers: string[]
  onSelectionChange: (memberId: string, selected: boolean) => void
  onSelectAll: (selected: boolean) => void
  onUpdateRoles: () => void
  onDeleteMembers: () => void
}

export function MembersTable({
  members,
  selectedMembers,
  onSelectionChange,
  onSelectAll,
  onUpdateRoles,
  onDeleteMembers,
}: MembersTableProps) {
  const allSelected = members.length > 0 && selectedMembers.length === members.length
  const someSelected = selectedMembers.length > 0 && selectedMembers.length < members.length

  return (
    <div className="space-y-2">
      {/* Selection Action Bar */}
      {selectedMembers.length > 0 && (
        <div className="bg-gray-800 border border-gray-700 rounded-lg p-4 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <Users className="w-5 h-5 text-blue-400" />
            <span className="text-white font-medium">
              {selectedMembers.length} member{selectedMembers.length !== 1 ? "s" : ""} selected
            </span>
          </div>
          <div className="flex items-center gap-2">
            <Button onClick={onUpdateRoles} variant="outline">
              Update Roles
            </Button>
            <Button onClick={onDeleteMembers} variant="destructive">
              Kick
            </Button>
          </div>
        </div>
      )}

      {/* Table Header */}
      <div className="grid grid-cols-7 gap-4 py-2 px-4 text-sm text-gray-400 font-medium border-b border-gray-700">
        <div className="flex items-center">
          <input
            type="checkbox"
            checked={allSelected}
            ref={(el) => {
              if (el) el.indeterminate = someSelected
            }}
            onChange={(e) => onSelectAll(e.target.checked)}
            className="rounded border-gray-600 bg-gray-800 text-blue-600 focus:ring-blue-500 focus:ring-offset-gray-900"
          />
        </div>
        <div>Name</div>
        <div>Email</div>
        <div>Role</div>
        <div>Last active</div>
        <div>Invited by</div>
        <div></div>
      </div>

      {/* Table Body */}
      <div className="space-y-1">
        {members.map((member) => (
          <MemberRow
            key={member.id}
            member={member}
            isSelected={selectedMembers.includes(member.id)}
            onSelectionChange={onSelectionChange}
          />
        ))}
      </div>
    </div>
  )
}
