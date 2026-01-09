import { Button } from "@/components/ui/button";
import { Empty, EmptyContent, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from "@/components/ui/empty";
import { Spinner } from "@/components/ui/spinner";
import { Inbox } from "lucide-react";


type Props = {
  isSearching?: boolean
  onCreateWorkspace?: () => void
}

export function WorkspaceEmptyState({ isSearching = false, onCreateWorkspace }: Props) {
  return (
    <Empty className="border-0 bg-transparent">
      <EmptyHeader>
        <EmptyMedia variant="icon">
          <Inbox className="size-6" />
        </EmptyMedia>
        <EmptyTitle>{isSearching ? "No results found" : "No workspaces yet"}</EmptyTitle>
      </EmptyHeader>
      <EmptyContent>
        <EmptyDescription>
          {isSearching ? "Try adjusting your search query or filters" : "Create your first workspace to get started"}
        </EmptyDescription>
        {!isSearching && onCreateWorkspace && (
          <Button
            onClick={onCreateWorkspace}
            className="mt-6 h-9 px-4 bg-primary hover:bg-primary/90 text-primary-foreground border-0 font-mono text-sm"
          >
            Create Workspace
          </Button>
        )}
      </EmptyContent>
    </Empty>
  )
}
export function WorkspaceLoadingState() {
  return (
    <div className="flex items-center justify-center h-full">
      <div className="flex flex-col items-center gap-3">
        <Spinner className="size-6" />
        <p className="text-sm font-mono text-muted-foreground">Loading workspaces...</p>
      </div>
    </div>
  )
}