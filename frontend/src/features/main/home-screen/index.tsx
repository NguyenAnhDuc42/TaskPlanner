import { CalenderTasks } from "./components/calender-tasks";
import { NotificationsList } from "./components/notifications-list";
import { WorkspaceList } from "./components/workspace-list";
import { Head } from "./components/head";
import { CreateWorkspaceForm } from "./components/create-workspace-form";
import { JoinWorkspaceDialog } from "./components/join-workspace-dialog";
import { useWorkspaceHome, useJoinWorkspaceByCode } from "./api";

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

  return (
    <div className="flex flex-col h-screen overflow-hidden bg-background">
      <Head />
      <div className="flex-1 grid grid-cols-4 gap-4 p-4 min-h-0">
        <div className="col-span-3 h-full min-h-0">
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
          />
        </div>
        <div className="col-span-1 flex flex-col gap-4 min-h-0">
          <div className="flex-1 min-h-0 outline-2 outline-border rounded-xl">
            <CalenderTasks />
          </div>
          <div className="flex-1 min-h-0 outline-2 outline-border rounded-xl">
            <NotificationsList />
          </div>
        </div>
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
