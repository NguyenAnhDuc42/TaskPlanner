import { useState } from "react";
import { useSidebarContext } from "@/features/workspace/components/sidebar-provider";
import { useCreateDashboard } from "../dashboard-api";
import { EntityLayerType } from "@/types/entity-layer-type";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Checkbox } from "@/components/ui/checkbox";
import { Button } from "@/components/ui/button";

export function CreateDashboardForm({ onSuccess }: { onSuccess: () => void }) {
  const { workspaceId } = useSidebarContext();
  const [name, setName] = useState("");
  const [isShared, setIsShared] = useState(false);
  const [isMain, setIsMain] = useState(false);
  const createDashboard = useCreateDashboard();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name || !workspaceId) return;

    await createDashboard.mutateAsync({
      layerId: workspaceId,
      layerType: EntityLayerType.ProjectWorkspace,
      name,
      isShared,
      isMain,
    });
    onSuccess();
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4 py-4">
      <div className="space-y-2">
        <Label
          htmlFor="name"
          className="text-[10px] uppercase font-bold tracking-widest text-[var(--theme-text-normal)]"
        >
          Dashboard Name
        </Label>
        <Input
          id="name"
          placeholder="e.g. Project Overview"
          className="bg-[var(--theme-item-normal)] border-border/10 text-[var(--theme-text-hover)] focus:ring-[var(--theme-bg-glow)]"
          value={name}
          onChange={(e) => setName(e.target.value)}
          autoFocus
        />
      </div>
      <div className="flex items-center justify-between">
        <Label
          htmlFor="shared"
          className="text-[10px] uppercase font-bold tracking-widest text-[var(--theme-text-normal)]"
        >
          Shared with workspace
        </Label>
        <Checkbox
          id="shared"
          checked={isShared}
          onCheckedChange={(checked) => setIsShared(!!checked)}
        />
      </div>
      <div className="flex items-center justify-between">
        <Label
          htmlFor="main"
          className="text-[10px] uppercase font-bold tracking-widest text-[var(--theme-text-normal)]"
        >
          Set as main
        </Label>
        <Checkbox
          id="main"
          checked={isMain}
          onCheckedChange={(checked) => setIsMain(!!checked)}
        />
      </div>
      <Button
        type="submit"
        className="w-full theme-selected border-0 transition-all hover:scale-[1.02]"
        disabled={createDashboard.isPending}
      >
        {createDashboard.isPending ? "Creating..." : "Create Dashboard"}
      </Button>
    </form>
  );
}
