"use client";

import { useMembers } from "./members-api";
import { MemberGridList } from "./member-components/member-grid-list";
import { useMemo } from "react";
import { useParams } from "@tanstack/react-router";

export default function MembersIndex() {
  const workspace = useParams({ from: "/workspaces/$workspaceId/members" });
  const { data, isLoading, fetchNextPage, hasNextPage, isFetchingNextPage } =  useMembers(workspace.workspaceId);

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
        // Add other handlers here as they are implemented
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
