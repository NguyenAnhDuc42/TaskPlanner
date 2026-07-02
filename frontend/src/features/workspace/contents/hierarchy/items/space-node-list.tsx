import { observer } from "mobx-react-lite";
import { useStore } from "@/stores/root.store";
import { SpaceNodeItem } from "./space-node-item";
import { SortableContext, verticalListSortingStrategy } from "@dnd-kit/sortable";

export const SpaceNodeList = observer(function SpaceNodeList({
  searchQuery,
}: {
  searchQuery: string;
}) {
  const rootStore = useStore();

  // Spaces are already fully hydrated locally (Bootstrap + Delta) — no pagination needed.
  const spaces = rootStore.spaceStore.all.sort((a, b) => ((a.orderKey ?? "") < (b.orderKey ?? "") ? -1 : 1));

  const filteredSpaces = searchQuery
    ? spaces.filter((s) => s.name.toLowerCase().includes(searchQuery.toLowerCase()))
    : spaces;

  return (
    <div className="flex flex-col">
      <SortableContext
        items={filteredSpaces.map((s) => `space-${s.id}`)}
        strategy={verticalListSortingStrategy}
      >
        {filteredSpaces.map((s) => (
          <SpaceNodeItem key={s.id} spaceId={s.id} isForcedOpen={!!searchQuery} />
        ))}
      </SortableContext>
    </div>
  );
});
