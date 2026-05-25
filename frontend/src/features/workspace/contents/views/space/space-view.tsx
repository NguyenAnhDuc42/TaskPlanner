import { EntityViewFrame } from "../entity-view-frame";

interface SpaceViewProps {
  spaceId: string;
}

export function SpaceView({ spaceId }: SpaceViewProps) {
  return (
    <EntityViewFrame
      topHeader={
        <div className="flex items-center justify-between w-full">
          <div>{/* Breadcrumbs will go here */} Space Breadcrumb</div>
          <div>{/* Actions will go here */} Actions</div>
        </div>
      }
      subHeader={
        <div className="flex gap-4">
          {/* Tabs will go here */}
          <button className="text-sm font-bold border-b-2 border-primary text-primary pb-2 mt-2">Overview</button>
          <button className="text-sm font-bold border-b-2 border-transparent text-muted-foreground hover:text-foreground pb-2 mt-2">Board</button>
        </div>
      }
    >
      <div className="h-full w-full p-4 flex flex-col">
        {/* Main Content Area (Overview or Kanban Board) */}
        <div className="flex-1 bg-muted/10 rounded-md border border-dashed border-border/50 flex items-center justify-center text-muted-foreground">
          Space Content Area (Overview/Board)
        </div>
      </div>
    </EntityViewFrame>
  );
}
