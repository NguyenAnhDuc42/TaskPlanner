"use client"

import type { UserSummary } from "@/types/user"
import { cn } from "@/lib/utils"

interface UserIconBarProps {
  users: UserSummary[]
  maxIcons?: number
  className?: string
}

export function UserIconBar({ users, maxIcons = 3, className }: UserIconBarProps) {
  const displayUsers = users.slice(0, maxIcons)
  const remainingUsers = users.length - displayUsers.length

  return (
    <div className={cn("group/icons flex items-center justify-start w-full", className)}>
      <div className="flex items-center transition-all duration-200 group-hover/icons:space-x-0 -space-x-2">
        {displayUsers.map((user, index) => (
          <div
            key={`${user.id}-${index}`}
            className="relative z-10 transition-all duration-200"
            style={{ zIndex: displayUsers.length - index }}
          >
            <div className="w-6 h-6 bg-blue-600 rounded-full flex items-center justify-center text-xs font-medium text-white border-2 border-gray-900">
              {user.name
                .split(" ")
                .map((n) => n[0])
                .join("")
                .toUpperCase()}
            </div>
          </div>
        ))}
        {remainingUsers > 0 && (
          <div className="relative z-0 w-6 h-6 bg-gray-600 rounded-full flex items-center justify-center text-xs font-medium text-white border-2 border-gray-900">
            +{remainingUsers}
          </div>
        )}
      </div>
    </div>
  )
}
