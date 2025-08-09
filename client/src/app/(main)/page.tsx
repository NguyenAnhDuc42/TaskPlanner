"use client";

import { useUser } from "@/features/auth/hooks";
import { useGetWorkspaces } from "@/features/user/user-hooks";
import { MainPageNavbar } from "./(component)/main-page-navbar";

import { AddWorkspaceCard } from "./(component)/add-workspace-card";
import { Skeleton } from "@/components/ui/skeleton";
import { WorkspaceDetail } from "@/types/workspace";
import { WorkspaceDetailCard } from "./(component)/workspace-detail-card";

export default function MainPage() {
  const { data: user, error: userError, isLoading: isUserLoading } = useUser();
  const {
    data: workspaces,
    error: workspacesError,
    isLoading: areWorkspacesLoading,
  } = useGetWorkspaces();

  // Error state or if user is not logged in
  if (userError || (!isUserLoading && !user)) {
    return (
      <div className="bg-black min-h-screen flex items-center justify-center text-white">
        <p>Could not load user data. Please try to login again.</p>
        {/* You can add a login button here */}
      </div>
    );
  }

  return (
    <div className="bg-background min-h-screen text-white">
      {isUserLoading || !user ? (
        <div className="bg-black border-b border-gray-800 sticky top-0 z-50">
          <div className="max-w-7xl mx-auto px-8 py-4">
            <div className="flex items-center justify-between gap-6">
              <Skeleton className="h-8 w-32 rounded-lg bg-gray-800" />
              <Skeleton className="h-10 w-full max-w-md rounded-lg bg-gray-800" />
              <div className="flex items-center gap-4">
                <Skeleton className="h-10 w-36 rounded-lg bg-gray-800" />
                <Skeleton className="h-10 w-10 rounded-full bg-gray-800" />
              </div>
            </div>
          </div>
        </div>
      ) : (
        <MainPageNavbar currentUser={user} />
      )}
      <main className="max-w-7xl mx-auto px-8 py-8">
        {isUserLoading || areWorkspacesLoading ? (
          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-8 items-start justify-items-center">
            {[...Array(8)].map((_, i) => (
              <Skeleton
                key={i}
                className="w-full max-w-[320px] h-[420px] rounded-lg bg-gray-800"
              />
            ))}
          </div>
        ) : workspacesError ? (
          <p className="text-red-500">Error: Could not load workspaces.</p>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-8 items-start justify-items-center">
            {workspaces?.map((workspace: WorkspaceDetail) => (
              <WorkspaceDetailCard key={workspace.id} workspace={workspace} />
            ))}
            <AddWorkspaceCard />
          </div>
        )}
      </main>
    </div>
  );
}