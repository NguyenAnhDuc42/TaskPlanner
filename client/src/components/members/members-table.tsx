import type { Member } from "@/types/user" // Import Member from types/user
import { MemberRow } from "./member-row"

interface MembersTableProps {
  members: Member[] // Uses the Member type with role as Role (string)
}

export function MembersTable({ members }: MembersTableProps) {
  return (
    <div className="space-y-2">
      <div className="grid grid-cols-6 gap-4 py-2 px-4 text-sm text-gray-400 font-medium border-b border-gray-700">
        <div>Name</div>
        <div>Email</div>
        <div>Role</div>
        <div>Last active</div>
        <div>Invited by</div>
        <div></div>
      </div>

      <div className="space-y-1">
        {members.map((member) => (
          <MemberRow key={member.id} member={member} />
        ))}
      </div>
    </div>
  )
}
