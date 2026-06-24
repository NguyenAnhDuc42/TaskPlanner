"use client";

import {
  useMembers,
  useAddMembers,
  useUpdateMembers,
  useRemoveMembers,
} from "./members-api";
import { MemberList, type MemberSavePayload } from "./member-components/member-list";
import { useMemo } from "react";
import { useParams } from "@tanstack/react-router";
import { useSelector } from "react-redux";
import { useUser } from "@/features/auth/auth-api";
import { useWorkspace } from "@/features/workspace/context/workspace-context";
import { ViewSkeleton } from "@/components/view-skeleton";
import { NotFoundScreen } from "@/components/not-found-screen";
import { memberSelectors } from "@/store/entityStore";
import type { RootState } from "@/store";
import type { MemberRecord } from "@/types/workspace/member-record";
import { Copy, Link2 } from "lucide-react";
import { toast } from "sonner";
import { copyToClipboard } from "@/lib/copy-to-clipboard";

export default function MembersIndex() {
  const { workspaceId } = useParams({
    from: "/workspaces/$workspaceId/members",
  });
  const { data: currentUser } = useUser();
  const { workspace } = useWorkspace();

  const { data, isLoading, isError, fetchNextPage, hasNextPage, isFetchingNextPage } =
    useMembers(workspaceId);

  const { mutate: addMembers, isPending: isAdding } = useAddMembers(workspaceId);
  const { mutate: updateMembers, isPending: isUpdating } = useUpdateMembers(workspaceId);
  const { mutate: removeMembers, isPending: isRemoving } = useRemoveMembers(workspaceId);

  const isSaving = isAdding || isUpdating || isRemoving;

  const memberIds = useMemo(
    () => data?.pages.flatMap((page) => page.items.map((i) => i.id)) ?? [],
    [data],
  );

  const members = useSelector((state: RootState) =>
    memberIds
      .map((id) => memberSelectors.selectById(state, id))
      .filter((r): r is MemberRecord => !!r),
  );

  const handleSave = async (payload: MemberSavePayload) => {
    const ops: Promise<unknown>[] = [];

    if (payload.adds.length > 0) {
      ops.push(
        addMembers({ members: payload.adds, enableEmail: false }),
      );
    }
    if (payload.updates.length > 0) {
      ops.push(updateMembers({ members: payload.updates }));
    }
    if (payload.removes.length > 0) {
      ops.push(removeMembers(payload.removes));
    }

    await Promise.all(ops);
  };

  if (isLoading && members.length === 0) return <ViewSkeleton />;
  if (isError) return <NotFoundScreen title="Failed to load members" description="Something went wrong loading the member list. Try refreshing the page." />;

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

      {hasNextPage && (
        <div className="flex justify-center p-4 border-t border-border">
          <button
            onClick={() => fetchNextPage()}
            disabled={isFetchingNextPage}
            className="text-[11px] text-primary hover:underline disabled:opacity-50 font-mono uppercase tracking-tight"
          >
            {isFetchingNextPage ? "Loading more..." : "Load more"}
          </button>
        </div>
      )}
    </div>
  );
}
