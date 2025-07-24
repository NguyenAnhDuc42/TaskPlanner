"use client"

import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"

import type { Member } from "@/types/user" // Import Member from types/user
import { RoleBadge } from "../custom/role-badge"

interface MemberRowProps {
  member: Member // Uses the Member type with role as Role (string)
}

export function MemberRow({ member }: MemberRowProps) {
  const getInitials = (name: string) => {
    return name
      .split(" ")
      .map((n) => n[0])
      .join("")
      .toUpperCase()
      .slice(0, 2)
  }

  return (
    <div className="grid grid-cols-6 gap-4 py-3 px-4 hover:bg-gray-800/50 rounded-lg">
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
        {/* Directly pass the role string from your backend data */}
        <RoleBadge role={member.role} />
      </div>

      <div className="flex items-center">
        <span className="text-gray-400">Jul 24</span>
      </div>

      <div className="flex items-center">
        <span className="text-gray-400">-</span>
      </div>

      {/* Removed the dropdown menu and any editing actions */}
      <div className="flex items-center justify-end">{/* Placeholder for any future actions, currently empty */}</div>
    </div>
  )
}
