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
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group"
import { Label } from "@/components/ui/label"
import { Role, mapRoleToBadge } from "@/utils/role-utils"
import type { UserSummary } from "@/types/user"

interface UpdateRolesDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  selectedMembers: UserSummary[]
  onUpdateRoles: (role: Role) => Promise<void>
  isLoading?: boolean
}

export function UpdateRolesDialog({
  open,
  onOpenChange,
  selectedMembers,
  onUpdateRoles,
  isLoading,
}: UpdateRolesDialogProps) {
  const [selectedRole, setSelectedRole] = useState<Role>(Role.Member)

  const handleUpdateRoles = async () => {
    try {
      await onUpdateRoles(selectedRole)
      onOpenChange(false)
    } catch (error) {
      console.error("Failed to update roles:", error)
    }
  }

  const allRoles = Object.values(Role) // ['Owner', 'Admin', 'Member', 'Guest']

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="bg-gray-900 border-gray-700 text-white sm:max-w-[475px]">
        <DialogHeader>
          <DialogTitle>Update Member Roles</DialogTitle>
          <DialogDescription className="text-gray-400">
            Update the role for {selectedMembers.length} selected member
            {selectedMembers.length !== 1 ? "s" : ""}.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-6 py-4">
          {/* Selected Members Preview */}
          <div className="space-y-2">
            <Label className="text-white">Selected Members ({selectedMembers.length})</Label>
            <div className="bg-gray-800 rounded-lg p-3 max-h-32 overflow-y-auto">
              {selectedMembers.map((member) => (
                <div key={member.id} className="flex items-center justify-between py-1">
                  <span className="text-sm text-gray-300">{member.name}</span>
                  <span className="text-xs text-gray-500">{member.role}</span>
                </div>
              ))}
            </div>
          </div>

          {/* Role Selection */}
          <div className="space-y-3">
            <Label className="text-white">New Role</Label>
            <RadioGroup
              value={selectedRole}
              onValueChange={(value) => setSelectedRole(value as Role)}
              className="space-y-3"
            >
              {allRoles.map((role) => {
                const { roleName, badgeClasses } = mapRoleToBadge(role)
                return (
                  <div key={role} className="flex items-start space-x-3">
                    <RadioGroupItem
                      value={role}
                      id={`role-${role}`}
                      className="mt-1 border-gray-600 text-white"
                    />
                    <div className="flex-1 space-y-1">
                      <div className="flex items-center gap-2">
                        <Label htmlFor={`role-${role}`} className="text-white cursor-pointer">
                          {roleName}
                        </Label>
                        <span
                          className={`px-2 py-1 rounded-full text-xs font-medium ${badgeClasses}`}
                        >
                          {roleName}
                        </span>
                      </div>
                      <p className="text-sm text-gray-400">
                        {`Grant ${roleName.toLowerCase()} permissions to selected members.`}
                      </p>
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
            onClick={handleUpdateRoles}
            disabled={isLoading}
            className="bg-blue-600 hover:bg-blue-700 text-white"
          >
            {isLoading ? "Updating..." : "Update Roles"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
