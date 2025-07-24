"use client"

import { Button } from "@/components/ui/button"
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu"
import { ChevronDown, Plus } from "lucide-react"

interface MembersFilterProps {
  totalCount: number
  selectedFilter: string
  onFilterChange: (filter: string) => void
  onInviteClick: () => void
}

export function MembersFilter({ totalCount, selectedFilter, onFilterChange, onInviteClick }: MembersFilterProps) {
  return (
    <div className="flex items-center justify-between py-4">
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button variant="outline" className="bg-gray-800 border-gray-600 text-white hover:bg-gray-700">
            All Users ({totalCount})
            <ChevronDown className="w-4 h-4 ml-2" />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent className="bg-gray-800 border-gray-600">
          <DropdownMenuItem onClick={() => onFilterChange("all")} className="text-white hover:bg-gray-700">
            All Users ({totalCount})
          </DropdownMenuItem>
          <DropdownMenuItem onClick={() => onFilterChange("owner")} className="text-white hover:bg-gray-700">
            Owners
          </DropdownMenuItem>
          <DropdownMenuItem onClick={() => onFilterChange("admin")} className="text-white hover:bg-gray-700">
            Admins
          </DropdownMenuItem>
          <DropdownMenuItem onClick={() => onFilterChange("member")} className="text-white hover:bg-gray-700">
            Members
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>

      <Button variant="ghost" onClick={onInviteClick} className="text-white hover:bg-gray-800">
        <Plus className="w-4 h-4 mr-2" />
        Invite people
      </Button>
    </div>
  )
}
