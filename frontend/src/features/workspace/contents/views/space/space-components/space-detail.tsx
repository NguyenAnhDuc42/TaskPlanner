import { observer } from "mobx-react-lite";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { SpaceDocumentsPanel } from "./space-documents-panel";

interface SpaceDetailProps {
  spaceId: string;
}

export const SpaceDetail = observer(function SpaceDetail({ spaceId }: SpaceDetailProps) {
  const rootStore = useWorkspaceRootStore();
  const space = rootStore.spaceStore.getById(spaceId);

  if (!space) return null;

  return (
    <div className="h-full w-full overflow-hidden">
      <SpaceDocumentsPanel spaceId={spaceId} />
    </div>
  );
});
