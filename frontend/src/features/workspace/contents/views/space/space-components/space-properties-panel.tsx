import { observer } from "mobx-react-lite";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { ChangesFeed, type ChangeEntry } from "@/features/workspace/components/changes-feed";

interface SpacePropertiesPanelProps {
  spaceId: string;
}

// Mocked until the backend change-log lands — see ChangesFeed.
const MOCK_CHANGES: ChangeEntry[] = [
  { id: "1", authorName: "You", message: "created this space", timestamp: "just now" },
];

function StatRow({ label, value }: { label: string; value: number }) {
  return (
    <div className="flex items-center justify-between h-6">
      <span className="text-[10px] font-semibold text-muted-foreground/50">{label}</span>
      <span className="text-[11px] font-bold text-foreground/80">{value}</span>
    </div>
  );
}

export const SpacePropertiesPanel = observer(function SpacePropertiesPanel({ spaceId }: Readonly<SpacePropertiesPanelProps>) {
  const rootStore = useWorkspaceRootStore();

  const tasks = rootStore.taskStore.getBySpace(spaceId).filter((t) => !t.parentTaskId);
  const folders = rootStore.folderStore.getBySpace(spaceId);
  const statuses = rootStore.statusStore.getVisibleForSpace(spaceId);

  const byStatus = statuses
    .map((s) => ({ name: s.name, color: s.color, count: tasks.filter((t) => t.statusId === s.id).length }))
    .filter((s) => s.count > 0);
  const unclassifiedCount = tasks.filter((t) => !t.statusId || !statuses.some((s) => s.id === t.statusId)).length;

  return (
    <div className="flex flex-col flex-1 min-h-0 overflow-y-auto p-3 gap-4 [&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20">
      <div className="flex flex-col gap-1">
        <span className="text-[10px] font-bold text-muted-foreground/60 uppercase tracking-wide">Overview</span>
        <div className="flex flex-col divide-y divide-border/20">
          <StatRow label="Tasks" value={tasks.length} />
          <StatRow label="Folders" value={folders.length} />
        </div>
      </div>

      <div className="flex flex-col gap-1">
        <span className="text-[10px] font-bold text-muted-foreground/60 uppercase tracking-wide">By Status</span>
        <div className="flex flex-col gap-1.5">
          {byStatus.map((s) => (
            <div key={s.name} className="flex items-center justify-between h-5">
              <span className="flex items-center gap-1.5 text-[10px] font-semibold text-foreground/70">
                <span className="h-2 w-2 rounded-full shrink-0" style={{ backgroundColor: s.color }} />
                {s.name}
              </span>
              <span className="text-[10px] font-bold text-muted-foreground/60">{s.count}</span>
            </div>
          ))}
          {unclassifiedCount > 0 && (
            <div className="flex items-center justify-between h-5">
              <span className="flex items-center gap-1.5 text-[10px] font-semibold text-muted-foreground/60">
                <span className="h-2 w-2 rounded-full shrink-0 bg-muted-foreground/40" />
                Unclassified
              </span>
              <span className="text-[10px] font-bold text-muted-foreground/60">{unclassifiedCount}</span>
            </div>
          )}
        </div>
      </div>

      <div className="flex flex-col gap-1.5">
        <span className="text-[10px] font-bold text-muted-foreground/60 uppercase tracking-wide">Changes</span>
        <ChangesFeed entries={MOCK_CHANGES} />
      </div>
    </div>
  );
});
