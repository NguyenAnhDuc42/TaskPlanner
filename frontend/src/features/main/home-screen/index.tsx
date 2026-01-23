import { CalenderTasks } from "./components/calender-tasks";
import { NotificationsList } from "./components/notifications-list";
import { WorkspaceList } from "./components/workspace-list";
import { useWorkspaces, useCreateWorkspace } from "./api";
import { Head } from "./components/head";
import { CreateWorkspaceForm } from "./components/create-workspace-form";
import React from "react";

export function WorkspaceHomeScreen() {
  const [filters, setFilters] = React.useState<{
    name?: string;
    variant?: string;
    owned?: boolean;
    isArchived?: boolean;
    direction?: "Ascending" | "Descending";
  }>({});

  const {
    data,
    isLoading: isWorkspacesLoading,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
  } = useWorkspaces(filters);

  const workspaces = React.useMemo(() => {
    return data?.pages.flatMap((page) => page.items) ?? [];
  }, [data]);

  const { mutate: create, isPending: isCreating } = useCreateWorkspace();
  const [isCreateModalOpen, setIsCreateModalOpen] = React.useState(false);

  return (
    <div className="flex flex-col h-screen overflow-hidden">
      <Head />
      <div className="flex-1 grid grid-flow-col grid-rows-2 gap-4 p-4 min-h-0">
        <div className="col-span-3 row-span-2 min-h-0">
          <WorkspaceList
            workspaces={workspaces}
            isLoading={isWorkspacesLoading}
            onCreateWorkspace={() => setIsCreateModalOpen(true)}
            onFetchNextPage={fetchNextPage}
            hasNextPage={hasNextPage}
            isFetchingNextPage={isFetchingNextPage}
            onSearchChange={(name) => setFilters((prev) => ({ ...prev, name }))}
            filters={filters}
            onFilterChange={(newFilters) =>
              setFilters((prev) => ({ ...prev, ...newFilters }))
            }
          />
        </div>
        <div className="col-span-1 row-span-1 outline-2">
          <CalenderTasks />
        </div>
        <div className="col-span-1 row-span-1 outline-2">
          <NotificationsList />
        </div>
      </div>

      <CreateWorkspaceForm
        open={isCreateModalOpen}
        onOpenChange={setIsCreateModalOpen}
        showTrigger={false}
        isLoading={isCreating}
        onSubmit={(data) => {
          create({ ...data, strictJoin: false });
          // Note: The form handles closing itself via internal onOpenChange/setOpen call in handleSubmit
        }}
      />
    </div>
  );
}
