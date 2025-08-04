import { Button } from "@/components/ui/button";
import { Plus } from "lucide-react";

export function AddWorkspaceCard({ onClick }: { onClick: () => void }) {
  return (
    
    <Button
        onClick={onClick}
      className="w-80 min-h-80 rounded-xl flex items-center justify-center 
                 border-2 border-dashed border-gray-300 dark:border-gray-700 
                 bg-gray-50/50 dark:bg-gray-900/50
                 text-gray-400 dark:text-gray-600 
                 hover:border-indigo-500 dark:hover:border-indigo-500
                 hover:text-indigo-500 dark:hover:text-indigo-500
                 hover:bg-indigo-50 dark:hover:bg-indigo-900/20
                 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500
                 transition-all duration-300 group"
    >
      <div className="text-center">
        <Plus className="h-16 w-16 mx-auto" />
        <p className="mt-2 font-semibold">Create New Workspace</p>
      </div>
    </Button>
  );
}
