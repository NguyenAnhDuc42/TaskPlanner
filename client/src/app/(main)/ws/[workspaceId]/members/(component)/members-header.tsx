"use client"

import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Search, Plus, Download } from "lucide-react"

interface MembersHeaderProps {
  searchQuery: string
  onSearchChange: (query: string) => void
  onInviteClick: () => void
  onExportClick: () => void
}

export function MembersHeader({ searchQuery, onSearchChange, onInviteClick, onExportClick }: MembersHeaderProps) {
  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <h1 className="text-2xl font-bold text-foreground">Manage people</h1>
          <Button variant="link" className="text-primary p-0 h-auto">
            Learn more
          </Button>
        </div>
        <Button variant="outline" size="sm" onClick={onExportClick}>
          <Download className="w-4 h-4 mr-2" />
          Export
        </Button>
      </div>

      <div className="flex items-center justify-between gap-4">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-muted-foreground w-4 h-4" />
          <Input placeholder="Search or invite by email" value={searchQuery} onChange={(e) => onSearchChange(e.target.value)} className="pl-10" />
        </div>
        <Button onClick={onInviteClick}>
          <Plus className="w-4 h-4 mr-2" />
          Invite people
        </Button>
      </div>
    </div>
  )
}
