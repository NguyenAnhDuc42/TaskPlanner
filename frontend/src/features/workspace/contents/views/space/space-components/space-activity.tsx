import { ChangesFeed } from "@/features/workspace/components/changes-feed";
import { useEntityChanges } from "@/features/workspace/components/use-entity-changes";

export function SpaceActivity({ spaceId }: Readonly<{ spaceId: string }>) {
  const { entries, isLoading } = useEntityChanges(spaceId, "Space");

  return (
    <div className="flex-1 overflow-y-auto px-8 py-4 [&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20">
      <ChangesFeed entries={entries} isLoading={isLoading} />
    </div>
  );
}
