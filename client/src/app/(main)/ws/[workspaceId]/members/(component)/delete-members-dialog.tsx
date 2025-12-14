"use client"

import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { AlertTriangle } from "lucide-react"

interface DeleteMembersDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  selectedMemberCount: number
  onConfirm: () => void
  isLoading?: boolean
}

export function DeleteMembersDialog({
  open,
  onOpenChange,
  selectedMemberCount,
  onConfirm,
  isLoading,
}: DeleteMembersDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[425px]">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <AlertTriangle className="text-destructive" />
            Confirm Removal
          </DialogTitle>
          <DialogDescription>
            Are you sure you want to remove {selectedMemberCount} member{selectedMemberCount !== 1 ? "s" : ""} from this workspace? This action cannot be undone.
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={isLoading}>
            Cancel
          </Button>
          <Button variant="destructive" onClick={onConfirm} disabled={isLoading}>
            {isLoading ? "Removing..." : `Remove Member${selectedMemberCount !== 1 ? "s" : ""}`}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}