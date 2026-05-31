import { useState } from "react";
import { useCreateSpaceMutation } from "../../contents/hierarchy/hierarchy-api";
import { Button } from "@/components/ui/button";
import { useWorkspace } from "../../context/workspace-provider";
import { toast } from "sonner";
import { PrivacyToggle, IconColorPicker, AttributeButton } from "./form-elements";
import { User } from "lucide-react";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { useUser } from "@/features/auth/api";

interface CreateSpaceFormProps {
  onSuccess?: (id: string) => void;
  onCancel?: () => void;
}

export function CreateSpaceForm({ onSuccess, onCancel }: CreateSpaceFormProps) {
  const { workspaceId, registry } = useWorkspace();
  const { data: currentUser } = useUser();
  const [createSpaceMutation, { isLoading: isCreating }] = useCreateSpaceMutation();
  const [name, setName] = useState("");
  const [isPrivate, setIsPrivate] = useState(false);
  const [icon, setIcon] = useState("LayoutGrid");
  const [color, setColor] = useState("#6366f1");
  const [selectedMemberIds, setSelectedMemberIds] = useState<string[]>([]);

  const handleToggleMember = (memberId: string) => {
    setSelectedMemberIds((prev) =>
      prev.includes(memberId)
        ? prev.filter((id) => id !== memberId)
        : [...prev, memberId]
    );
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim()) return;

    try {
      const result = await createSpaceMutation({
        workspaceId,
        body: { 
          name, 
          isPrivate, 
          color, 
          icon, 
          memberIdsToInvite: selectedMemberIds 
        },
      }).unwrap();
      toast.success("Space created");
      
      // Reset form state cleanly upon success
      setName("");
      setIsPrivate(false);
      setIcon("LayoutGrid");
      setColor("#6366f1");
      setSelectedMemberIds([]);

      onSuccess?.((result as any).id);
    } catch (error) {
      toast.error("Failed to create space");
    }
  };

  return (
    <form onSubmit={handleSubmit} className="flex flex-col w-full">
      {/* Main Header / Input Section */}
      <div className="px-3 pt-2.5 pb-2">
        <div className="flex items-center gap-3">
          <IconColorPicker 
            icon={icon} 
            color={color} 
            onChange={(i, c) => { setIcon(i); setColor(c); }} 
          />
          <input
            placeholder="Space name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            className="flex-1 bg-transparent border-none focus:ring-0 text-[13px] font-semibold placeholder:text-muted-foreground/30 py-0 outline-none tracking-tight"
            autoFocus
          />
        </div>
      </div>

      {/* Attribute Strip */}
      <div className="px-3 py-1.5 flex flex-nowrap items-center gap-1.5 border-t border-border/5 overflow-x-auto [&::-webkit-scrollbar]:hidden">
        <PrivacyToggle isPrivate={isPrivate} onChange={setIsPrivate} />
        
        <Popover>
          <PopoverTrigger asChild>
            <AttributeButton icon={User} className="ml-auto" active={selectedMemberIds.length > 0}>
              {selectedMemberIds.length > 0 ? `Members (${selectedMemberIds.length})` : "Members"}
            </AttributeButton>
          </PopoverTrigger>
          <PopoverContent className="w-52 p-1 bg-popover border border-border/40 shadow-lg rounded-md text-foreground" align="end">
            <div className="text-[9px] font-bold text-muted-foreground/60 p-1 border-b border-border/10 uppercase tracking-wider font-mono">
              Invite Workspace Members
            </div>
            <div className="max-h-[180px] overflow-y-auto mt-1 flex flex-col gap-0.5 [&::-webkit-scrollbar]:w-1 [&::-webkit-scrollbar-thumb]:bg-white/[0.05]">
              {Object.values(registry.memberMap)
                .filter((member: any) => {
                  const isCurrentUser = member.email && currentUser?.email && member.email.toLowerCase() === currentUser.email.toLowerCase();
                  return !isCurrentUser;
                })
                .map((member: any) => {
                  const isSelected = selectedMemberIds.includes(member.id || member.workspaceMemberId);
                  const initials = (member.name || "U").split(" ").map((w: string) => w[0]).slice(0, 2).join("").toUpperCase();
                  return (
                    <button
                      key={member.id || member.workspaceMemberId}
                      type="button"
                      onClick={() => handleToggleMember(member.id || member.workspaceMemberId)}
                      className="flex items-center gap-1.5 p-1 rounded hover:bg-white/[0.03] transition-colors w-full text-left cursor-pointer"
                    >
                      <input
                        type="checkbox"
                        checked={isSelected}
                        readOnly
                        className="h-3 w-3 rounded border-border/40 accent-primary cursor-pointer"
                      />
                      <div className="h-4.5 w-4.5 rounded-full bg-primary/20 border border-border/20 flex items-center justify-center text-[8px] font-black text-white shrink-0">
                        {initials}
                      </div>
                      <div className="flex-1 min-w-0">
                        <div className="font-bold text-[9px] text-foreground/90 truncate leading-none">{member.name}</div>
                        <div className="text-[7.5px] text-muted-foreground/45 truncate mt-0.5">{member.email}</div>
                      </div>
                    </button>
                  );
                })}
            </div>
          </PopoverContent>
        </Popover>
      </div>

      {/* Footer */}
      <div className="px-3 py-1.5 bg-background flex items-center justify-end gap-2 border-t border-border/10">
        <Button
          type="button"
          variant="ghost"
          size="sm"
          onClick={onCancel}
          className="h-7 px-2.5 text-[10px] font-medium text-muted-foreground hover:text-foreground"
        >
          Cancel
        </Button>
        <Button
          type="submit"
          size="sm"
          disabled={!name.trim() || isCreating}
          className="h-7 px-4 text-[10px] font-semibold bg-primary hover:bg-primary/90 text-primary-foreground shadow-sm rounded-md transition-all active:scale-95"
        >
          {isCreating ? "Creating..." : "Create Space"}
        </Button>
      </div>
    </form>
  );
}

