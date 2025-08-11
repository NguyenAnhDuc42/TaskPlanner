"use client"

import { useState, useEffect } from "react"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { X, Plus } from "lucide-react"
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group"
import { Checkbox } from "@/components/ui/checkbox"

import { Role, mapRoleToBadge } from "@/utils/role-utils"

interface DialogEmail {
  email: string
  isSelected: boolean
}

interface InviteMembersDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onInvite: (data: { emails: string[]; role: Role }) => Promise<void>
  isLoading?: boolean
}

export function InviteMembersDialog({ open, onOpenChange, onInvite, isLoading }: InviteMembersDialogProps) {
  const [emailInput, setEmailInput] = useState("")
  const [dialogEmails, setDialogEmails] = useState<DialogEmail[]>([])
  const [selectedRole, setSelectedRole] = useState<Role>(Role.Member)
  const [inputError, setInputError] = useState("")

  useEffect(() => {
    if (!open) {
      setEmailInput("")
      setDialogEmails([])
      setSelectedRole(Role.Member)
      setInputError("")
    }
  }, [open])

  const isValidEmail = (email: string) => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)

  const handleAddEmail = () => {
    const trimmedEmail = emailInput.trim()
    if (!trimmedEmail) {
      setInputError("Email cannot be empty.")
      return
    }
    if (!isValidEmail(trimmedEmail)) {
      setInputError("Please enter a valid email address.")
      return
    }
    if (dialogEmails.some((e) => e.email === trimmedEmail)) {
      setInputError("This email has already been added.")
      return
    }
    setDialogEmails((prev) => [...prev, { email: trimmedEmail, isSelected: true }])
    setEmailInput("")
    setInputError("")
  }

  const handleToggleEmailSelection = (emailToToggle: string) => {
    setDialogEmails((prev) =>
      prev.map((e) => (e.email === emailToToggle ? { ...e, isSelected: !e.isSelected } : e))
    )
  }

  const handleRemoveEmail = (emailToRemove: string) => {
    setDialogEmails((prev) => prev.filter((e) => e.email !== emailToRemove))
  }

  const handleFinishInvite = async () => {
    const emailsToInvite = dialogEmails.filter((e) => e.isSelected).map((e) => e.email)
    if (emailsToInvite.length === 0) {
      setInputError("Please select at least one email to invite.")
      return
    }
    try {
      await onInvite({ emails: emailsToInvite, role: selectedRole })
      const remainingEmails = dialogEmails.filter((e) => !e.isSelected)
      setDialogEmails(remainingEmails)
      setSelectedRole(Role.Member)
      setInputError("")
      if (remainingEmails.length === 0) {
        onOpenChange(false)
      }
    } catch (error) {
      console.error("Invitation failed:", error)
    }
  }

  const allRoles = Object.values(Role) // ['Owner', 'Admin', 'Member', 'Guest']
  const hasSelectedEmails = dialogEmails.some((e) => e.isSelected)

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[475px] flex flex-col max-h-[85vh]">
        <DialogHeader>
          <DialogTitle>Invite Members</DialogTitle>
          <DialogDescription>Add emails, select who to invite, and assign a role.</DialogDescription>
        </DialogHeader>

        <div className="flex-1 flex flex-col gap-4 overflow-hidden py-2">
          {/* Email Input */}
          <div className="space-y-2 shrink-0">
            <Label htmlFor="email-input">Email address</Label>
            <div className="flex gap-2">
              <Input
                id="email-input"
                placeholder="Enter email"
                value={emailInput}
                onChange={(e) => {
                  setEmailInput(e.target.value)
                  setInputError("")
                }}
                onKeyDown={(e) => {
                  if (e.key === "Enter") {
                    e.preventDefault()
                    handleAddEmail()
                  }
                }}
                className="flex-1"
              />
              <Button variant="outline" onClick={handleAddEmail}>
                <Plus className="w-4 h-4 mr-2" /> Add
              </Button>
            </div>
            {inputError && <p className="text-sm text-destructive">{inputError}</p>}
          </div>

          {/* Emails List */}
          {dialogEmails.length > 0 && (
            <div className="space-y-2 flex flex-col min-h-0">
              <Label>Emails to invite ({dialogEmails.length})</Label>
              <div className="flex-1 flex flex-col gap-2 overflow-y-auto pr-2">
                {dialogEmails.map((dialogEmail) => (
                  <div key={dialogEmail.email} className="flex items-center gap-2">
                    <Checkbox
                      id={`email-${dialogEmail.email}`}
                      checked={dialogEmail.isSelected}
                      onCheckedChange={() => handleToggleEmailSelection(dialogEmail.email)}
                    />
                    <Label htmlFor={`email-${dialogEmail.email}`} className="flex-1 cursor-pointer">
                      {dialogEmail.email}
                    </Label>
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-6 w-6 p-0 text-muted-foreground hover:text-foreground"
                      onClick={() => handleRemoveEmail(dialogEmail.email)}
                    >
                      <X className="h-4 w-4" />
                    </Button>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Role Selection */}
          <div className="space-y-3 shrink-0">
            <Label>Assign role to selected emails</Label>
            <RadioGroup
              value={selectedRole}
              onValueChange={(value) => setSelectedRole(value as Role)}
              className="space-y-3"
            >
              {allRoles.map((role) => {
                const { roleName, badgeClasses } = mapRoleToBadge(role)
                return (
                  <div key={role} className="flex items-start space-x-3">
                    <RadioGroupItem value={role} id={`role-${role}`} className="mt-1" />
                    <div className="flex-1 space-y-1">
                      <Label htmlFor={`role-${role}`} className="cursor-pointer flex items-center gap-2">
                        <span className={`px-2 py-0.5 rounded text-xs ${badgeClasses}`}>{roleName}</span>
                      </Label>
                      <p className="text-sm text-muted-foreground">
                        {`Grant ${roleName.toLowerCase()} permissions to selected users.`}
                      </p>
                    </div>
                  </div>
                )
              })}
            </RadioGroup>
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button onClick={handleFinishInvite} disabled={isLoading || !hasSelectedEmails}>
            {isLoading ? "Inviting..." : "Finish"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
