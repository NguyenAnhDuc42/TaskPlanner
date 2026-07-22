import { useEffect, useMemo, useRef, useState } from "react";
import { observer } from "mobx-react-lite";
import { Orbit, Plus } from "lucide-react";
import { DynamicIcon } from "@/components/dynamic-icon";
import { useNavigate, useLocation } from "@tanstack/react-router";
import { useWorkspace } from "@/features/workspace/context/workspace-context";
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useSyncEngine } from "@/sync/sync-provider";
import { SpaceMutations } from "@/mutations/space.mutations";
import { SpaceContextMenu } from "../hierarchy-components/context-menus/space-context-menu";
import { EntityMenuTrigger } from "../hierarchy-components/context-menus/shared";
import { MoreVertical } from "lucide-react";
import { cn } from "@/lib/utils";
import { toast } from "sonner";
import { extractErrorMessage } from "@/types/api-error";

function ProjectNodeItem({ spaceId, collapsed }: { spaceId: string; collapsed?: boolean }) {
  const { workspaceId } = useWorkspace();
  const rootStore = useWorkspaceRootStore();
  const navigate = useNavigate();
  const pathname = useLocation({ select: (l) => l.pathname });

  const space = rootStore.spaceStore.getById(spaceId);
  if (!space) return null;

  const isActive = pathname.includes(`/spaces/${space.id}`);

  if (collapsed) {
    return (
      <SpaceContextMenu spaceId={space.id} spaceName={space.name}>
        <button
          type="button"
          title={space.name}
          onClick={() =>
            navigate({ to: "/workspaces/$workspaceId/spaces/$spaceId", params: { workspaceId, spaceId: space.id } })
          }
          className={cn(
            "flex items-center justify-center h-7 w-7 rounded-md transition-colors cursor-pointer",
            isActive ? "bg-primary/10" : "hover:bg-muted/50",
          )}
        >
          <DynamicIcon name={space.icon || "Orbit"} size={14} color={space.color || "#ffffff"} className="shrink-0" />
        </button>
      </SpaceContextMenu>
    );
  }

  return (
    <SpaceContextMenu spaceId={space.id} spaceName={space.name}>
      <div
        className={cn(
          "flex items-center gap-2 h-7 px-2 rounded-md transition-colors cursor-pointer group border border-transparent",
          isActive
            ? "bg-primary/10 text-primary"
            : "text-muted-foreground hover:bg-muted/50 hover:text-foreground",
        )}
        onClick={() =>
          navigate({ to: "/workspaces/$workspaceId/spaces/$spaceId", params: { workspaceId, spaceId: space.id } })
        }
      >
        <DynamicIcon name={space.icon || "Orbit"} size={14} color={space.color || "#ffffff"} className="shrink-0" />
        <span className="flex-1 text-[11px] font-semibold truncate">{space.name}</span>
        <EntityMenuTrigger>
          <button
            type="button"
            className="h-4 w-4 flex items-center justify-center rounded-sm opacity-0 group-hover:opacity-100 hover:bg-background/60 text-muted-foreground hover:text-foreground transition-colors shrink-0"
            onClick={(e) => e.stopPropagation()}
          >
            <MoreVertical className="h-3.5 w-3.5" />
          </button>
        </EntityMenuTrigger>
      </div>
    </SpaceContextMenu>
  );
}

export const ProjectNodeList = observer(function ProjectNodeList({ collapsed }: { collapsed?: boolean }) {
  const rootStore = useWorkspaceRootStore();
  const syncEngine = useSyncEngine();
  const { canCreateSpace } = useWorkspaceRole();
  const spaceMutations = useMemo(() => new SpaceMutations(rootStore, syncEngine), [rootStore, syncEngine]);

  const [isCreating, setIsCreating] = useState(false);
  const [name, setName] = useState("");
  const inputRef = useRef<HTMLInputElement>(null);
  const submittedRef = useRef(false);

  useEffect(() => {
    if (!isCreating) return;
    const raf = requestAnimationFrame(() => inputRef.current?.focus());
    return () => cancelAnimationFrame(raf);
  }, [isCreating]);

  const handleCreate = () => {
    if (submittedRef.current) return;
    submittedRef.current = true;
    const trimmed = name.trim();
    setIsCreating(false);
    setName("");
    if (trimmed) {
      spaceMutations
        .create({ name: trimmed, isPrivate: false, color: "#ffffff", icon: "Orbit" })
        .catch((err) => toast.error(extractErrorMessage(err, "Failed to create project")));
    }
    setTimeout(() => { submittedRef.current = false; }, 300);
  };

  const spaces = rootStore.spaceStore.allSorted;

  if (collapsed) {
    return (
      <div className="flex flex-col gap-0.5">
        {spaces.map((s) => (
          <ProjectNodeItem key={s.id} spaceId={s.id} collapsed />
        ))}
        {canCreateSpace && (
          <button
            type="button"
            title="Add project"
            onClick={() => spaceMutations
              .create({ name: "New Project", isPrivate: false, color: "#ffffff", icon: "Orbit" })
              .catch((err) => toast.error(extractErrorMessage(err, "Failed to create project")))}
            className="flex items-center justify-center h-7 w-7 rounded-md text-muted-foreground/50 hover:text-foreground hover:bg-muted/50 transition-colors cursor-pointer"
          >
            <Plus className="h-3.5 w-3.5" />
          </button>
        )}
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-0.5">
      {spaces.map((s) => (
        <ProjectNodeItem key={s.id} spaceId={s.id} />
      ))}

      {canCreateSpace && (
        isCreating ? (
          <div className="flex items-center gap-2 h-7 px-2 rounded-md border border-primary/40 bg-primary/5">
            <Orbit className="h-3.5 w-3.5 shrink-0" color="#ffffff" />
            <input
              ref={inputRef}
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "Enter") e.currentTarget.blur();
                if (e.key === "Escape") { setIsCreating(false); setName(""); }
              }}
              onBlur={handleCreate}
              placeholder="Project name..."
              className="flex-1 text-[11px] font-semibold bg-transparent border-none outline-none placeholder:text-muted-foreground/40"
            />
          </div>
        ) : (
          <button
            type="button"
            onClick={() => setIsCreating(true)}
            className="flex items-center gap-2 h-7 px-2 rounded-md text-muted-foreground/50 hover:text-foreground hover:bg-muted/50 transition-colors cursor-pointer"
          >
            <Plus className="h-3.5 w-3.5 shrink-0" />
            <span className="text-[11px] font-semibold">Add Project</span>
          </button>
        )
      )}
    </div>
  );
});
