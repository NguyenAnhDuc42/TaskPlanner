"use client";

import {
  useMembers,
  useAddMembers,
  useUpdateMembers,
  useRemoveMembers,
} from "./members-api";
import { MemberGridList } from "./member-components/member-grid-list";
import { useMemo, useState } from "react";
import { useParams } from "@tanstack/react-router";
import { AddMembersForm } from "./member-components/add-members-form";
import type { Role } from "@/types/role";
import type { MembershipStatus } from "@/types/membership-status";

export default function MembersIndex() {
  const { workspaceId } = useParams({
    from: "/workspaces/$workspaceId/members",
  });
  const [isAddMemberOpen, setIsAddMemberOpen] = useState(false);

  const { data, isLoading, fetchNextPage, hasNextPage, isFetchingNextPage } =
    useMembers(workspaceId);
  const { mutateAsync: addMembers, isPending: isAddingMembers } =
    useAddMembers(workspaceId);
  const { mutateAsync: updateMembers } = useUpdateMembers(workspaceId);
  const { mutateAsync: removeMembers } = useRemoveMembers(workspaceId);

  const members = useMemo(() => {
    return data?.pages.flatMap((page) => page.items) ?? [];
  }, [data]);

  const handleBatchUpdate = async (
    ids: string[],
    role?: Role,
    status?: MembershipStatus,
  ) => {
    await updateMembers({
      members: ids.map((id) => ({
        userId: id,
        role: role as any,
        status: status as any,
      })),
    });
  };

  const handleRemoveMembers = async (ids: string[]) => {
    await removeMembers(ids);
  };

  if (isLoading && members.length === 0) {
    return (
      <div className="flex items-center justify-center p-8 text-muted-foreground animate-pulse">
        Loading members...
      </div>
    );
  }

  return (
    <div className="h-full">
      <MemberGridList
        members={members}
        onAddMember={() => setIsAddMemberOpen(true)}
        onEditMember={(id, role) => handleBatchUpdate([id], role as Role)}
        onDeleteMember={(id) => handleRemoveMembers([id])}
        onBatchUpdate={handleBatchUpdate}
        onBatchDelete={handleRemoveMembers}
      />

      <AddMembersForm
        open={isAddMemberOpen}
        onOpenChange={setIsAddMemberOpen}
        onSubmit={addMembers}
        isLoading={isAddingMembers}
      />

      {/* Simple infinite scroll trigger for now */}
      {hasNextPage && (
        <div className="flex justify-center p-4">
          <button
            onClick={() => fetchNextPage()}
            disabled={isFetchingNextPage}
            className="text-sm text-primary hover:underline disabled:opacity-50"
          >
            {isFetchingNextPage ? "Loading more..." : "Load more"}
          </button>
        </div>
      )}
    </div>
  );
}
