"use client"

import { useState } from "react"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Textarea } from "@/components/ui/textarea"
import { Label } from "@/components/ui/label"

interface InviteMembersDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onInvite: (emails: string[]) => void
  isLoading?: boolean
}

export function InviteMembersDialog({ open, onOpenChange, onInvite, isLoading }: InviteMembersDialogProps) {
  const [emailText, setEmailText] = useState("")

  const handleInvite = () => {
    const emails = emailText
      .split(/[,\n]/)
      .map((email) => email.trim())
      .filter((email) => email.length > 0)

    if (emails.length > 0) {
      onInvite(emails)
      setEmailText("")
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="bg-gray-900 border-gray-700 text-white">
        <DialogHeader>
          <DialogTitle>Invite Members</DialogTitle>
          <DialogDescription className="text-gray-400">
            Invite members to your workspace by entering their email addresses.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="emails" className="text-white">
              Email addresses
            </Label>
            <Textarea
              id="emails"
              placeholder="Enter email addresses separated by commas or new lines"
              value={emailText}
              onChange={(e) => setEmailText(e.target.value)}
              className="min-h-[100px] bg-gray-800 border-gray-600 text-white placeholder:text-gray-400"
            />
          </div>
        </div>

        <DialogFooter>
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
            className="bg-transparent border-gray-600 text-gray-300 hover:bg-gray-800"
          >
            Cancel
          </Button>
          <Button
            onClick={handleInvite}
            disabled={isLoading || !emailText.trim()}
            className="bg-white text-black hover:bg-gray-100"
          >
            {isLoading ? "Inviting..." : "Invite"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
