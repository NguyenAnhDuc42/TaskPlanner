import { useEffect, useMemo, useRef, useState } from "react";
import { createFileRoute, useNavigate, useParams } from "@tanstack/react-router";
import { observer } from "mobx-react-lite";
import { Plus, LayoutGrid } from "lucide-react";
import { toast } from "sonner";
import { workspaceSearchSchema } from "../workspace-search-schema";
import { LoadingScreen } from "@/components/loading-screen";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useSyncEngine, useSyncReady } from "@/sync/sync-provider";
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";
import { SpaceMutations } from "@/mutations/space.mutations";
import { extractErrorMessage } from "@/types/api-error";

// No real "hub" page exists yet — land on the last-visited space (or the first one), same
// resolution IconRail's "Projects" button already uses (workspace-layout.tsx's handleSelectIcon).
// If the workspace has no spaces at all, offer to create the first one right here instead of
// just saying "go use the sidebar."
const WorkspaceIndexRedirect = observer(function WorkspaceIndexRedirect() {
  const { workspaceId } = useParams({ from: "/workspaces/$workspaceId/" });
  const navigate = useNavigate();
  const rootStore = useWorkspaceRootStore();
  const syncEngine = useSyncEngine();
  const { ready } = useSyncReady();
  const { canCreateSpace } = useWorkspaceRole();
  const spaceMutations = useMemo(() => new SpaceMutations(rootStore, syncEngine), [rootStore, syncEngine]);
  const spaces = rootStore.spaceStore.all;

  const [isCreating, setIsCreating] = useState(false);
  const [name, setName] = useState("");
  const inputRef = useRef<HTMLInputElement>(null);
  const submittedRef = useRef(false);

  useEffect(() => {
    if (!ready || spaces.length === 0) return;
    const lastSpaceId = localStorage.getItem(`lastSpaceId:${workspaceId}`);
    const targetSpaceId = lastSpaceId && spaces.some((s) => s.id === lastSpaceId)
      ? lastSpaceId
      : [...spaces].sort((a, b) => ((a.orderKey ?? "") < (b.orderKey ?? "") ? -1 : 1))[0].id;

    navigate({
      to: "/workspaces/$workspaceId/spaces/$spaceId",
      params: { workspaceId, spaceId: targetSpaceId },
      replace: true,
    });
  }, [ready, spaces, workspaceId, navigate]);

  useEffect(() => {
    if (isCreating) inputRef.current?.focus();
  }, [isCreating]);

  const handleCreate = () => {
    if (submittedRef.current) return;
    submittedRef.current = true;
    const trimmed = name.trim();
    setIsCreating(false);
    setName("");
    if (trimmed) {
      spaceMutations.create({ name: trimmed, isPrivate: false, color: "#6366f1", icon: "LayoutGrid" })
        .catch((err) => toast.error(extractErrorMessage(err, "Failed to create space")));
    }
    setTimeout(() => { submittedRef.current = false; }, 300);
  };

  if (!ready) return <LoadingScreen />;

  if (spaces.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center h-full text-center px-6 gap-3">
        <div className="h-10 w-10 rounded-lg bg-primary/10 text-primary flex items-center justify-center">
          <LayoutGrid className="h-5 w-5" />
        </div>
        <div className="flex flex-col gap-1">
          <p className="text-sm font-semibold text-foreground/80">No spaces yet</p>
          <p className="text-xs text-muted-foreground max-w-xs">
            {canCreateSpace ? "Create your first space to start organizing tasks." : "Ask a workspace admin to create a space."}
          </p>
        </div>

        {canCreateSpace && (
          isCreating ? (
            <input
              ref={inputRef}
              value={name}
              onChange={(e) => setName(e.target.value)}
              onBlur={handleCreate}
              onKeyDown={(e) => {
                if (e.key === "Enter") handleCreate();
                if (e.key === "Escape") { setIsCreating(false); setName(""); }
              }}
              placeholder="Space name..."
              className="h-8 w-56 text-xs font-medium px-3 rounded-md border border-border/60 bg-muted/20 outline-none focus:border-primary/50 text-center"
            />
          ) : (
            <button
              type="button"
              onClick={() => setIsCreating(true)}
              className="flex items-center gap-1.5 h-8 px-3 rounded-md bg-primary/10 text-primary text-xs font-semibold hover:bg-primary/20 transition-colors cursor-pointer"
            >
              <Plus className="h-3.5 w-3.5" />
              Create Space
            </button>
          )
        )}
      </div>
    );
  }

  return <LoadingScreen />;
});

export const Route = createFileRoute("/workspaces/$workspaceId/")({
  validateSearch: (search) => workspaceSearchSchema.parse(search),
  pendingComponent: LoadingScreen,
  component: WorkspaceIndexRedirect,
});
