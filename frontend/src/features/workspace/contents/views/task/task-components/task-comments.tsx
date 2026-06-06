import { useState } from "react";
import { useSelector } from "react-redux";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Send } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { memberSelectors } from "@/store/entityStore";
import { useWorkspace } from "@/features/workspace/context/workspace-provider";
import { useGetTaskCommentsQuery, useAddCommentMutation } from "../task-api";

interface TaskCommentsProps {
  taskId: string;
}

export function TaskComments({ taskId }: TaskCommentsProps) {
  const { registry } = useWorkspace();
  const allMembers = useSelector(memberSelectors.selectAll);

  const { data: comments = [] } = useGetTaskCommentsQuery(taskId, {
    skip: !taskId,
  });

  const [addComment] = useAddCommentMutation();
  const [newCommentText, setNewCommentText] = useState("");

  const handleSendComment = async (e?: React.FormEvent) => {
    e?.preventDefault();
    if (!newCommentText.trim()) return;
    try {
      await addComment({ taskId, content: newCommentText.trim() }).unwrap();
      setNewCommentText("");
    } catch {}
  };

  return (
    <div className="space-y-4 pt-6 border-t border-border/30">
      <h3 className="font-mono text-[10px] uppercase tracking-widest text-muted-foreground/70">
        Comments
      </h3>

      <div className="space-y-4 max-h-[300px] overflow-y-auto pr-2 [&::-webkit-scrollbar]:w-1 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20 hover:[&::-webkit-scrollbar-thumb]:bg-muted-foreground/45 [&::-webkit-scrollbar-track]:bg-transparent">
        {comments.map((comment) => {
          const creator = registry.memberMap[comment.creatorId] || allMembers.find((m) => m.id === comment.creatorId || m.workspaceMemberId === comment.creatorId);
          const name = creator?.name || "Unknown User";
          const initials = name.split(" ").map((n: string) => n[0]).join("").slice(0, 2).toUpperCase();
          return (
            <div key={comment.id} className="flex items-start gap-3 text-sm">
              <Avatar className="h-6 w-6 mt-0.5 shrink-0">
                {creator?.avatarUrl && <AvatarImage src={creator.avatarUrl} alt={name} />}
                <AvatarFallback className="text-[8px] bg-primary/20 text-primary">{initials}</AvatarFallback>
              </Avatar>
              <div className="flex-1 space-y-1">
                <div className="flex items-baseline gap-2">
                  <span className="font-semibold text-foreground text-xs">{name}</span>
                  <span className="text-[10px] text-muted-foreground">
                    {new Date(comment.createdAt).toLocaleString()}
                  </span>
                </div>
                <p className="text-muted-foreground text-xs leading-relaxed">{comment.content}</p>
              </div>
            </div>
          );
        })}
        {comments.length === 0 && (
          <p className="text-xs text-muted-foreground/50 italic py-2">No comments yet. Start the conversation!</p>
        )}
      </div>

      <form onSubmit={handleSendComment} className="flex gap-2 mt-2">
        <Input
          placeholder="Write a comment..."
          value={newCommentText}
          onChange={(e) => setNewCommentText(e.target.value)}
          className="text-xs h-9 bg-muted/20 border-none"
        />
        <Button type="submit" size="icon" className="h-9 w-9 shrink-0" disabled={!newCommentText.trim()}>
          <Send className="h-4 w-4" />
        </Button>
      </form>
    </div>
  );
}
