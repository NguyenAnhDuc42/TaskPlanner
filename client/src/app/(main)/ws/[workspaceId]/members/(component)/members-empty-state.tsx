"use client"

import { Button } from "@/components/ui/button"
import { Users } from "lucide-react"

interface MembersEmptyStateProps {
  title: string
  description: string
  actionText?: string
  onActionClick?: () => void
}

export function MembersEmptyState({ title, description, actionText, onActionClick }: MembersEmptyStateProps) {
  return (
    <div className="text-center py-16 px-6 bg-card rounded-lg border border-dashed border-border">
      <div className="mx-auto h-12 w-12 flex items-center justify-center rounded-full bg-muted">
        <Users className="h-6 w-6 text-muted-foreground" />
      </div>
      <h3 className="mt-4 text-lg font-semibold text-foreground">{title}</h3>
      <p className="mt-2 text-sm text-muted-foreground">{description}</p>
      {actionText && onActionClick && (
        <div className="mt-6">
          <Button onClick={onActionClick}>{actionText}</Button>
        </div>
      )}
    </div>
  )
}