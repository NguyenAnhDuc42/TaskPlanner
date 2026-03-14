import { WorkspaceList } from "./components/workspace-list";
import { Head } from "./components/head";
import { LeftSidebar } from "./components/left-sidebar";
import { WorkspaceDetails } from "./components/workspace-details";
import { CreateWorkspaceForm } from "./components/create-workspace-form";
import { JoinWorkspaceDialog } from "./components/join-workspace-dialog";
import { useWorkspaceHome, useJoinWorkspaceByCode } from "./api";
import * as React from "react";
import type { WorkspaceSummary } from "./type";

export function WorkspaceHomeScreen() {
  const {
    filters,
    workspaces,
    isWorkspacesLoading,
    isCreating,
    isCreateModalOpen,
    setIsCreateModalOpen,
    isJoinModalOpen,
    setIsJoinModalOpen,
    hasNextPage,
    isFetchingNextPage,
    fetchNextPage,
    handleCreateWorkspace,
    handleJoinWorkspace,
    handlePinWorkspace,
    handleSearchChange,
    handleFilterChange,
  } = useWorkspaceHome();

  const { mutate: joinByCode, isPending: isJoining } = useJoinWorkspaceByCode();
  const [selectedWorkspace, setSelectedWorkspace] = React.useState<WorkspaceSummary | null>(null);

  return (
    <div className="flex flex-col h-screen overflow-hidden bg-background">
      <Head />
      <div className="flex-1 flex overflow-hidden gap-4 p-4 min-h-0">
        {/* Left: Schedule & Notifications */}
        <LeftSidebar />

        {/* Center: Workspace Listing */}
        <div className="flex-1 min-w-0 flex flex-col h-full">
          <WorkspaceList
            workspaces={workspaces}
            isLoading={isWorkspacesLoading}
            onCreateWorkspace={() => setIsCreateModalOpen(true)}
            onJoinWorkspace={handleJoinWorkspace}
            onPinWorkspace={handlePinWorkspace}
            onFetchNextPage={fetchNextPage}
            hasNextPage={hasNextPage}
            isFetchingNextPage={isFetchingNextPage}
            onSearchChange={handleSearchChange}
            filters={filters}
            onFilterChange={handleFilterChange}
            selectedWorkspaceId={selectedWorkspace?.id}
            onSelectWorkspace={setSelectedWorkspace}
          />
        </div>

        {/* Right: Detailed Analytics & Quick Management */}
        {selectedWorkspace && (
           <div className="animate-in fade-in slide-in-from-right-4 duration-300">
             <WorkspaceDetails workspace={selectedWorkspace} />
           </div>
        )}
      </div>

      <CreateWorkspaceForm
        open={isCreateModalOpen}
        onOpenChange={setIsCreateModalOpen}
        showTrigger={false}
        isLoading={isCreating}
        onSubmit={handleCreateWorkspace}
      />

      <JoinWorkspaceDialog
        open={isJoinModalOpen}
        onOpenChange={setIsJoinModalOpen}
        isLoading={isJoining}
        onJoin={(code) => joinByCode(code)}
      />
    </div>
  );
}
