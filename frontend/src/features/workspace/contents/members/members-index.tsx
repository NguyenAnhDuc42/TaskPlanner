"use client";

import { useMembers, useAddMembers } from "./members-api";
import { MemberGridList } from "./member-components/member-grid-list";
import { useMemo, useState } from "react";
import { useParams } from "@tanstack/react-router";
import { AddMembersForm } from "./member-components/add-members-form";

export default function MembersIndex() {
  const { workspaceId } = useParams({
    from: "/workspaces/$workspaceId/members",
  });
  const [isAddMemberOpen, setIsAddMemberOpen] = useState(false);

  const { data, isLoading, fetchNextPage, hasNextPage, isFetchingNextPage } =
    useMembers(workspaceId);
  const { mutateAsync: addMembers, isPending: isAddingMembers } =
    useAddMembers(workspaceId);

  const members = useMemo(() => {
    return data?.pages.flatMap((page) => page.items) ?? [];
  }, [data]);

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
