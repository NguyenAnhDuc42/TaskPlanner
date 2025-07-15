import { Button } from "@/components/ui/button";
import { ListNode } from "@/features/workspace/workspacetype";
import { cn } from "@/lib/utils";
import { List, MoreHorizontal, Plus } from "lucide-react";


export type ListContext = {
  spaceId: string;
  folderId?: string;
  listId: string;
}
interface ListItemProps {list: ListNode,context: ListContext, isHighlighted?: boolean}

export function ListItem({ list, context ,isHighlighted }: ListItemProps) {
  return (
    <div className={cn("group flex items-center gap-2 py-1 px-2 rounded-sm cursor-pointer","hover:bg-neutral-800", isHighlighted && "bg-neutral-800",)}>
      <div className="w-6 flex-shrink-0 flex items-center justify-center">
        <List className="h-4 w-4 text-gray-400" />
      </div>
      <span className="flex-1 text-sm text-neutral-200 truncate">{list.name}</span>
      <div className="flex items-center gap-1">
        <Button variant="ghost" size="sm" className="h-6 w-6 p-0 text-neutral-400 hover:bg-neutral-700">
          <MoreHorizontal className="h-4 w-4" />
        </Button>
        <Button variant="ghost" size="sm" className="h-6 w-6 p-0 text-neutral-400 hover:bg-neutral-700">
          <Plus className="h-4 w-4" />
        </Button>
      </div>
    </div>
  )
}