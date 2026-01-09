import type { WorkspaceSummary } from "../type";
import { Search, X } from "lucide-react";
import { WorkspaceItem } from "./workspace-item";
import React from "react";
import {
  WorkspaceEmptyState,
  WorkspaceLoadingState,
} from "./pending-workspace";
import { ScrollArea } from "@/components/ui/scroll-area";

type Props = {
  workspaces: WorkspaceSummary[];
  isLoading?: boolean;
  onCreateWorkspace?: () => void;
  onWorkspaceOpen?: (id: string) => void;
  onFetchNextPage?: () => void;
  hasNextPage?: boolean;
  isFetchingNextPage?: boolean;
};

export function WorkspaceList({ workspaces,isLoading, onCreateWorkspace, onWorkspaceOpen,
 onFetchNextPage, hasNextPage, isFetchingNextPage }: Props) {
  const [searchQuery, setSearchQuery] = React.useState("");
  const observerRef = React.useRef<HTMLDivElement>(null);

  React.useEffect(() => {
    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting && hasNextPage && !isFetchingNextPage) {
          onFetchNextPage?.();
        }
      },
      { threshold: 0.1 }
    );

    if (observerRef.current) {
      observer.observe(observerRef.current);
    }

    return () => observer.disconnect();
  }, [hasNextPage, isFetchingNextPage, onFetchNextPage]);

  const filteredWorkspaces = React.useMemo(() => {
    if (!searchQuery.trim()) return workspaces;
    const query = searchQuery.toLowerCase();
    return workspaces.filter(
      (ws) =>
        ws.name?.toLowerCase().includes(query) ||
        String(ws.variant).toLowerCase().includes(query) ||
        String(ws.role).toLowerCase().includes(query)
    );
  }, [workspaces, searchQuery]);

  if (isLoading) {
    return <WorkspaceLoadingState />;
  }

  return (
    <div className="h-full bg-background flex flex-col outline-2">
      <div className="border-b border-border px-6 py-6">
        <div className="flex items-center gap-4">
          <button
            onClick={onCreateWorkspace}
            className="flex items-center gap-2 h-9 px-4 bg-primary hover:bg-primary/90 text-primary-foreground border-0 font-mono text-sm rounded-md"
          >
            Create
          </button>

          <div className="flex-1" />

          <div className="relative w-72">
            <div className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground">
              <Search className="h-4 w-4" />
            </div>
            <input
              type="text"
              placeholder="Search workspaces..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="w-full h-9 pl-10 pr-9 bg-card border border-border text-foreground placeholder-muted-foreground font-mono text-sm focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary/30 transition-colors"
            />
            {searchQuery && (
              <button
                onClick={() => setSearchQuery("")}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors"
              >
                <X className="h-4 w-4" />
              </button>
            )}
          </div>
        </div>
      </div>

      <div className="flex-1 overflow-hidden flex flex-col min-h-0">
        <div className="flex-1 flex flex-col min-h-0">
          {filteredWorkspaces.length === 0 && !isLoading ? (
            <WorkspaceEmptyState
              isSearching={!!searchQuery}
              onCreateWorkspace={onCreateWorkspace}
            />
          ) : (
            <ScrollArea className="flex-1 w-full min-h-0">
              <div className="p-4 space-y-2">
                {filteredWorkspaces.map((workspace) => (
                  <div key={workspace.id}>
                    <WorkspaceItem
                      workspaceSummary={workspace}
                      onOpen={onWorkspaceOpen}
                    />
                  </div>
                ))}

                {/* Infinite scroll sentinel */}
                <div ref={observerRef} className="h-4 w-full" />

                {isFetchingNextPage && (
                  <div className="py-4 flex justify-center">
                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-primary" />
                  </div>
                )}
              </div>
            </ScrollArea>
          )}
        </div>
      </div>
    </div>
  );
}
