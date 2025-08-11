"use client"

import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Checkbox } from "@/components/ui/checkbox"
import { RoleBadge } from "../../../../../../components/custom/role-badge"
import type { UserSummary } from "@/types/user"

interface MemberRowProps {
  member: UserSummary
  isSelected: boolean
  onSelectionChange: (memberId: string, selected: boolean) => void
}

export function MemberRow({ member, isSelected, onSelectionChange }: MemberRowProps) {
  const getInitials = (name: string) => {
    return name
      .split(" ")
      .map((n) => n[0])
      .join("")
      .toUpperCase()
      .slice(0, 2)
  }

  return (
    <div className="grid grid-cols-7 gap-4 py-3 px-4 hover:bg-gray-800/50 rounded-lg">
      <div className="flex items-center">
        <Checkbox
          checked={isSelected}
          onCheckedChange={(checked) => onSelectionChange(member.id, checked as boolean)}
          className="border-gray-600 data-[state=checked]:bg-white data-[state=checked]:text-black"
        />
      </div>

      <div className="flex items-center gap-3">
        <Avatar className="w-8 h-8">
          <AvatarImage src={`/placeholder.svg?height=32&width=32&text=${getInitials(member.name)}`} />
          <AvatarFallback className="bg-blue-600 text-white text-sm">{getInitials(member.name)}</AvatarFallback>
        </Avatar>
        <span className="text-white font-medium">{member.name}</span>
      </div>

      <div className="flex items-center">
        <span className="text-gray-300">{member.email}</span>
      </div>

      <div className="flex items-center">
        <RoleBadge role={member.role} />
      </div>

      <div className="flex items-center">
        <span className="text-gray-400">Jul 24</span>
      </div>

      <div className="flex items-center">
        <span className="text-gray-400">-</span>
      </div>

      <div className="flex items-center justify-end">{/* Placeholder for any future actions */}</div>
    </div>
  )
}
