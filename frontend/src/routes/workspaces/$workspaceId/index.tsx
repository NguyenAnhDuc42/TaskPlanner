import { createFileRoute } from "@tanstack/react-router";
import { workspaceSearchSchema } from "../$workspaceId";
import { Info } from "lucide-react";

export const Route = createFileRoute("/workspaces/$workspaceId/")({
  validateSearch: (search) => workspaceSearchSchema.parse(search),
  component: WorkspaceIndex,
});

function WorkspaceIndex() {
  return (
    <div className="flex flex-col items-center justify-center min-h-[500px] text-center gap-6 animate-in fade-in slide-in-from-bottom-4 duration-700">
      <div className="p-6 rounded-full bg-[var(--theme-item-normal)] border border-border/10">
        <Info className="h-10 w-10 text-[var(--theme-text-hover)] opacity-60" />
      </div>
      <div className="space-y-1.5">
        <h3 className="text-lg font-bold uppercase tracking-[0.3em] text-[var(--theme-text-hover)]">
          Workspace Hub
        </h3>
        <p className="text-[10px] font-bold uppercase tracking-[0.1em] text-[var(--theme-text-normal)] opacity-40 italic">
          Select a project or space from the sidebar to begin.
        </p>
      </div>
    </div>
  );
}
