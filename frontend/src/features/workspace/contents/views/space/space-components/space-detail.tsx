import { useSpaceDetail } from "../space-api";
import { SpaceDocumentsPanel } from "./space-documents-panel";

interface SpaceDetailProps {
  spaceId: string;
}

export function SpaceDetail({ spaceId }: SpaceDetailProps) {
  const space = useSpaceDetail(spaceId);

  if (!space) return null;

  return (
    <div className="h-full w-full overflow-hidden">
      <SpaceDocumentsPanel spaceId={spaceId} />
    </div>
  );
}
