"use client";

import { useMemo, useState } from "react";
import { observer } from "mobx-react-lite";
import { MemberList, type MemberSavePayload } from "./member-components/member-list";
import { useUser } from "@/features/auth/auth-api";
import { useWorkspace } from "@/features/workspace/context/workspace-context";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { MemberMutations } from "@/mutations/member.mutations";
import { extractErrorMessage } from "@/types/api-error";
import { Copy, Link2 } from "lucide-react";
import { toast } from "sonner";
import { copyToClipboard } from "@/lib/copy-to-clipboard";

export default observer(function MembersIndex() {
  const { data: currentUser } = useUser();
  const { workspace } = useWorkspace();
  const rootStore = useWorkspaceRootStore();
  const memberMutations = useMemo(() => new MemberMutations(rootStore), [rootStore]);
  const [isSaving, setIsSaving] = useState(false);

  // Plain read, not useMemo — this is a mobx-react-lite observer, which tracks observable reads
  // made directly during render (see FavoriteNodeList for the full rationale). Members are fully
  // hydrated locally by Bootstrap + Delta, unlike the old paginated fetch — no loading/cursor state
  // needed.
  const members = rootStore.memberStore.all;

  const handleSave = async (payload: MemberSavePayload) => {
    setIsSaving(true);
    try {
      const ops: Promise<unknown>[] = [];
      if (payload.adds.length > 0) ops.push(memberMutations.add(payload.adds));
      if (payload.updates.length > 0) ops.push(memberMutations.update(payload.updates));
      if (payload.removes.length > 0) ops.push(memberMutations.remove(payload.removes));

      await Promise.all(ops);
      toast.success("Members updated successfully");
    } catch (error) {
      toast.error(extractErrorMessage(error, "Failed to update members"));
      throw error;
    } finally {
      setIsSaving(false);
    }
  };

  const joinCode = workspace?.joinCode;
  const canSeeCode = workspace?.canInvite;

  return (
    <div className="h-full flex flex-col">
      {/* Invite code banner — only for admins/owners */}
      {canSeeCode && joinCode && (
        <div className="shrink-0 flex items-center gap-3 px-4 py-2 border-b border-border/30 bg-muted/10">
          <Link2 className="h-3.5 w-3.5 text-muted-foreground/50 shrink-0" />
          <span className="text-[10px] text-muted-foreground/60 font-medium">Invite code</span>
          <code className="text-[11px] font-mono font-bold text-foreground/90 tracking-widest bg-muted/40 px-2 py-0.5 rounded-md border border-border/30">
            {joinCode}
          </code>
          <button
            type="button"
            onClick={() => {
              copyToClipboard(joinCode);
              toast.success("Invite code copied");
            }}
            className="flex items-center gap-1 h-6 px-2 text-[10px] font-semibold text-muted-foreground hover:text-foreground hover:bg-muted/50 rounded-md border border-border/30 transition-colors shrink-0"
          >
            <Copy className="h-3 w-3" />
            Copy
          </button>
        </div>
      )}

      <MemberList members={members} currentUserId={currentUser?.id} isSaving={isSaving} onSave={handleSave} />
    </div>
  );
});
