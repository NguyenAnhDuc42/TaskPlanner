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
import { getRoleProperties, type Role } from "@/utils/role-utils" // Import Role type
import { Checkbox } from "@/components/ui/checkbox" // Import Checkbox

interface DialogEmail {
  email: string
  isSelected: boolean
}

interface InviteMembersDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onInvite: (data: { emails:string[]; role: Role }) => Promise<void> // Updated to handle async and control dialog flow
  isLoading?: boolean
}

export function InviteMembersDialog({ open, onOpenChange, onInvite, isLoading }: InviteMembersDialogProps) {
  const [emailInput, setEmailInput] = useState("")
  const [dialogEmails, setDialogEmails] = useState<DialogEmail[]>([]) // Stores emails with selection state
  const [selectedRole, setSelectedRole] = useState<Role>("member") // Default role
  const [inputError, setInputError] = useState("")

  // Reset state when dialog opens/closes
  useEffect(() => {
    if (!open) {
      setEmailInput("")
      setDialogEmails([])
      setSelectedRole("member")
      setInputError("")
    }
  }, [open])

  const isValidEmail = (email: string) => {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)
  }

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

    setDialogEmails((prev) => [...prev, { email: trimmedEmail, isSelected: true }]) // New emails are selected by default
    setEmailInput("")
    setInputError("")
  }

  const handleToggleEmailSelection = (emailToToggle: string) => {
    setDialogEmails((prev) => prev.map((e) => (e.email === emailToToggle ? { ...e, isSelected: !e.isSelected } : e)))
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
      await onInvite({ emails: emailsToInvite, role: selectedRole });

      const remainingEmails = dialogEmails.filter((e) => !e.isSelected);
      setDialogEmails(remainingEmails);
      setSelectedRole("member") // Reset role after successful invite
      setInputError("")

      if (remainingEmails.length === 0) {
        onOpenChange(false);
      }
    } catch (error) {
      // Error is thrown by the parent, so we just log it. The UI will not change.
      console.error("Invitation failed:", error)
    }
  }

  const allRoles: Role[] = ["owner", "admin", "member", "guest"]
  const hasSelectedEmails = dialogEmails.some((e) => e.isSelected)

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="bg-gray-900 border-gray-700 text-white sm:max-w-[475px]">
        <DialogHeader>
          <DialogTitle>Invite Members</DialogTitle>
          <DialogDescription className="text-gray-400">
            Add emails, select who to invite, and assign a role.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-6 py-2">
          {/* Email Input and Add Button */}
          <div className="space-y-2">
            <Label htmlFor="email-input" className="text-white">
              Email address
            </Label>
            <div className="flex gap-2">
              <Input
                id="email-input"
                placeholder="Enter email"
                value={emailInput}
                onChange={(e) => {
                  setEmailInput(e.target.value)
                  setInputError("") // Clear error on input change
                }}
                onKeyDown={(e) => {
                  if (e.key === "Enter") {
                    e.preventDefault() // Prevent form submission
                    handleAddEmail()
                  }
                }}
                className="flex-1 bg-gray-800 border-gray-600 text-white placeholder:text-gray-400"
              />
              <Button
                variant="outline"
                onClick={handleAddEmail}
                className="bg-transparent border-gray-600 text-gray-300 hover:bg-gray-800"
              >
                <Plus className="w-4 h-4 mr-2" /> Add
              </Button>
            </div>
            {inputError && <p className="text-red-400 text-sm">{inputError}</p>}
          </div>

          {/* Added Emails List with Checkboxes */}
          {dialogEmails.length > 0 && (
            <div className="space-y-2">
              <Label className="text-white">Emails to invite ({dialogEmails.length})</Label>
              <div className="flex flex-col gap-2 max-h-[150px] overflow-y-auto pr-2">
                {dialogEmails.map((dialogEmail) => (
                  <div key={dialogEmail.email} className="flex items-center gap-2">
                    <Checkbox
                      id={`email-${dialogEmail.email}`}
                      checked={dialogEmail.isSelected}
                      onCheckedChange={() => handleToggleEmailSelection(dialogEmail.email)}
                      className="border-gray-600 data-[state=checked]:bg-white data-[state=checked]:text-black"
                    />
                    <Label htmlFor={`email-${dialogEmail.email}`} className="flex-1 text-white cursor-pointer">
                      {dialogEmail.email}
                    </Label>
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-6 w-6 p-0 text-gray-400 hover:text-white"
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
          <div className="space-y-3">
            <Label className="text-white">Assign role to selected emails</Label>
            <RadioGroup
              value={selectedRole}
              onValueChange={(value) => setSelectedRole(value as Role)}
              className="space-y-3"
            >
              {allRoles.map((role) => {
                const { label, description } = getRoleProperties(role)
                return (
                  <div key={role} className="flex items-start space-x-3">
                    <RadioGroupItem value={role} id={`role-${role}`} className="mt-1 border-gray-600 text-white" />
                    <div className="flex-1 space-y-1">
                      <Label htmlFor={`role-${role}`} className="text-white cursor-pointer">
                        {label}
                      </Label>
                      <p className="text-sm text-gray-400">{description}</p>
                    </div>
                  </div>
                )
              })}
            </RadioGroup>
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
            onClick={handleFinishInvite}
            disabled={isLoading || !hasSelectedEmails} // Disable if no emails are selected
            className="bg-white text-black hover:bg-gray-100"
          >
            {isLoading ? "Inviting..." : "Finish"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
