"use client";

import {
  useMembers,
  useAddMembers,
  useUpdateMembers,
  useRemoveMembers,
} from "./members-api";
import { MemberList } from "./member-components/member-list";
import { useMemo, useState } from "react";
import { useParams } from "@tanstack/react-router";
import { AddMembersForm } from "./member-components/add-members-form";
import type { Role } from "@/types/role";
import type { MembershipStatus } from "@/types/membership-status";
import { useSelector } from "react-redux";
import { memberSelectors } from "@/store/entityStore";
import type { RootState } from "@/store";
import type { MemberRecord } from "@/types/workspace/member-record";

export default function MembersIndex() {
  const { workspaceId } = useParams({
    from: "/workspaces/$workspaceId/members",
  });
  const [isAddMemberOpen, setIsAddMemberOpen] = useState(false);

  const { data, isLoading, fetchNextPage, hasNextPage, isFetchingNextPage } =
    useMembers(workspaceId);
  const { mutate: addMembers, isPending: isAddingMembers } =
    useAddMembers(workspaceId);
  const { mutate: updateMembers } = useUpdateMembers(workspaceId);
  const { mutate: removeMembers } = useRemoveMembers(workspaceId);

  const memberIds = useMemo(() => {
    return data?.pages.flatMap((page) => page.items.map((i) => i.id)) ?? [];
  }, [data]);

  const members = useSelector((state: RootState) =>
    memberIds
      .map((id) => memberSelectors.selectById(state, id))
      .filter((record): record is MemberRecord => !!record)
  );

  const handleBatchUpdate = async (
    ids: string[],
    role?: Role,
    status?: MembershipStatus,
  ) => {
    await updateMembers({
      members: ids.map((id) => ({
        userId: id,
        role: role,
        status: status,
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
      <MemberList
        members={members}
        onAddMember={() => setIsAddMemberOpen(true)}
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
