import { CreateWorkspaceDialog } from "@/components/custom-form/buttons/create-workspace-dialog";
import { Button } from "@/components/ui/button";
import { Plus } from "lucide-react";

export function AddWorkspaceCard() {
  return (
    <CreateWorkspaceDialog>
      <Button
        className="w-full max-w-[320px] min-h-[475px] rounded-xl flex items-center justify-center 
                 border-2 border-dashed border-border
                 bg-card/50
                 text-muted-foreground
                 hover:border-primary hover:text-primary
                 hover:bg-primary/10
                 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary
                 transition-all duration-300 group"
      >
        <div className="text-center">
          <Plus className="h-16 w-16 mx-auto" />
          <p className="mt-2 font-semibold">Create New Workspace</p>
        </div>
      </Button>
    </CreateWorkspaceDialog>
  );
}
