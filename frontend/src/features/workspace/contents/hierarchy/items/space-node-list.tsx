import { observer } from "mobx-react-lite";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { SpaceNodeItem } from "./space-node-item";
import { SortableContext, verticalListSortingStrategy } from "@dnd-kit/sortable";

export const SpaceNodeList = observer(function SpaceNodeList() {
  const rootStore = useWorkspaceRootStore();

  // Spaces are already fully hydrated locally (Bootstrap + Delta) — no pagination needed.
  const spaces = rootStore.spaceStore.all.sort((a, b) => ((a.orderKey ?? "") < (b.orderKey ?? "") ? -1 : 1));

  return (
    <div className="flex flex-col">
      <SortableContext
        items={spaces.map((s) => `space-${s.id}`)}
        strategy={verticalListSortingStrategy}
      >
        {spaces.map((s) => (
          <SpaceNodeItem key={s.id} spaceId={s.id} isForcedOpen={false} />
        ))}
      </SortableContext>
    </div>
  );
});
