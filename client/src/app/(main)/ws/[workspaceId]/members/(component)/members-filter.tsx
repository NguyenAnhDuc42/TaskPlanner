"use client"

import { Button } from "@/components/ui/button"
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu"
import { Role } from "@/utils/role-utils"
import { ChevronDown } from "lucide-react"

interface MembersFilterProps {
  filteredCount: number
  selectedFilter: string
  totalCount: number
  onFilterChange: (filter: Role | "all") => void
  onInviteClick: () => void
}

export function MembersFilter({
  filteredCount,
  selectedFilter,
  totalCount,
  onFilterChange,
}: MembersFilterProps) {
  const filterLabels: Record<string, string> = {
    all: "All Users",
    [Role.Owner]: "Owners",
    [Role.Admin]: "Admins",
    [Role.Member]: "Members",
    [Role.Guest]: "Guests",
  }

  const buttonLabel = `${filterLabels[selectedFilter] || "All Users"} (${filteredCount})`

  return (
    <div className="flex items-center justify-start py-4">
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button variant="outline" className="capitalize">
            {buttonLabel}
            <ChevronDown className="w-4 h-4 ml-2" />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent>
          <DropdownMenuItem onClick={() => onFilterChange("all")}>All Users ({totalCount})</DropdownMenuItem>
          <DropdownMenuItem onClick={() => onFilterChange(Role.Owner)}>Owners</DropdownMenuItem>
          <DropdownMenuItem onClick={() => onFilterChange(Role.Admin)}>Admins</DropdownMenuItem>
          <DropdownMenuItem onClick={() => onFilterChange(Role.Member)}>Members</DropdownMenuItem>
          <DropdownMenuItem onClick={() => onFilterChange(Role.Guest)}>Guests</DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
    </div>
  )
}
