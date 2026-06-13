import { useState } from "react";
import { useSelector } from "react-redux";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Send, CornerDownRight, Trash2, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { memberSelectors, commentSelectors } from "@/store/entityStore";
import { toast } from "sonner";
import { useGetTaskCommentsQuery, useAddCommentMutation, useDeleteCommentMutation } from "../task-api";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";

interface TaskCommentsProps {
  taskId: string;
}

export function TaskComments({ taskId }: Readonly<TaskCommentsProps>) {
  const members = useSelector(memberSelectors.selectEntities);
  const allMembers = useSelector(memberSelectors.selectAll);

  useGetTaskCommentsQuery(taskId, {
    skip: !taskId,
  });

  const allComments = useSelector(commentSelectors.selectAll);
  const comments = allComments
    .filter(c => c.taskId === taskId)
    .sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime());

  const [addComment] = useAddCommentMutation();
  const [deleteComment] = useDeleteCommentMutation();
  const [newCommentText, setNewCommentText] = useState("");
  const [replyingTo, setReplyingTo] = useState<{ id: string, name: string, content: string } | null>(null);
  const [deleteCommentId, setDeleteCommentId] = useState<string | null>(null);

  const handleSendComment = async (e?: React.FormEvent) => {
    e?.preventDefault();
    if (!newCommentText.trim()) return;
    try {
      await addComment({ taskId, content: newCommentText.trim(), parentCommentId: replyingTo?.id }).unwrap();
      setNewCommentText("");
      setReplyingTo(null);
    } catch {
      toast.error("Failed to add comment. Please try again.");
      }
  };

  const handleDeleteComment = (commentId: string) => {
    setDeleteCommentId(commentId);
  };

  const confirmDelete = () => {
    if (deleteCommentId) {
      deleteComment({ taskId, commentId: deleteCommentId });
      setDeleteCommentId(null);
    }
  };

  return (
    <div className="space-y-4 pt-6 border-t border-border/30">
      <h3 className="font-mono text-[10px] uppercase tracking-widest text-muted-foreground/70">
        Comments
      </h3>

      <div className="space-y-4 max-h-[300px] overflow-y-auto pr-2 [&::-webkit-scrollbar]:w-1 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20 hover:[&::-webkit-scrollbar-thumb]:bg-muted-foreground/45 [&::-webkit-scrollbar-track]:bg-transparent">
        {comments.map((comment) => {
          const creator = members[comment.creatorId] || allMembers.find((m) => m.id === comment.creatorId || m.workspaceMemberId === comment.creatorId);
          const name = creator?.name || "Unknown User";
          const initials = name.split(" ").map((n: string) => n[0]).join("").slice(0, 2).toUpperCase();
          const parentComment = comment.parentCommentId ? comments.find(c => c.id === comment.parentCommentId) : null;
          const parentMember = parentComment ? allMembers.find(m => m.id === parentComment.creatorId || m.workspaceMemberId === parentComment.creatorId) : null;
          const parentName = parentMember ? parentMember.name : "Unknown User";

          return (
            <div key={comment.id} className="flex gap-3 group">
              <Avatar className="h-7 w-7 mt-0.5 shrink-0 rounded-md">
                {creator?.avatarUrl && <AvatarImage src={creator.avatarUrl} alt={name} />}
                <AvatarFallback className="text-[10px] bg-primary/20 text-primary font-bold rounded-md">{initials}</AvatarFallback>
              </Avatar>
              <div className="flex flex-col gap-1 min-w-0 flex-1">
                <div className="flex items-center gap-2">
                  <span className="font-semibold text-foreground text-xs">{name}</span>
                  <span className="text-[10px] text-muted-foreground/60 font-medium">
                    {new Date(comment.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                  </span>
                </div>

                {parentComment && (
                  <div className="flex flex-col gap-0.5 pl-2.5 py-1.5 border-l-2 border-primary/30 bg-muted/20 rounded-r-md mt-0.5 mb-1.5">
                    <span className="text-[9px] font-bold text-primary/80 flex items-center gap-1">
                      <CornerDownRight className="h-2 w-2" /> Replying to {parentName}
                    </span>
                    <span className="text-[10px] text-muted-foreground truncate opacity-80 italic line-clamp-2 whitespace-normal">{parentComment.content}</span>
                  </div>
                )}

                <div className="bg-muted/30 rounded-md px-3 py-2 text-xs text-foreground/90 leading-relaxed border border-border/5">
                  {comment.content}
                </div>
                <div className="flex items-center gap-3 pt-0.5 opacity-0 group-hover:opacity-100 transition-opacity">
                  <button 
                    type="button"
                    onClick={() => setReplyingTo({ id: comment.id, name, content: comment.content })}
                    className="text-[10px] font-semibold text-muted-foreground hover:text-foreground flex items-center gap-1 transition-colors"
                  >
                    <CornerDownRight className="h-3 w-3" /> Reply
                  </button>
                  <button 
                    type="button"
                    onClick={() => handleDeleteComment(comment.id)}
                    className="text-[10px] font-semibold text-muted-foreground hover:text-destructive flex items-center gap-1 transition-colors"
                  >
                    <Trash2 className="h-3 w-3" /> Delete
                  </button>
                </div>
              </div>
            </div>
          );
        })}
        {comments.length === 0 && (
          <p className="text-xs text-muted-foreground/50 italic py-2">No comments yet. Start the conversation!</p>
        )}
      </div>

      <div className="mt-2 relative bg-muted/10 rounded-md border border-border/40 p-1 flex flex-col gap-1 shadow-sm">
        {replyingTo && (
          <div className="flex items-center justify-between px-2 py-1.5 bg-muted/40 rounded-sm border border-border/20 mx-1 mt-1">
            <div className="flex flex-col gap-0.5 overflow-hidden">
              <span className="text-[9px] font-bold text-primary flex items-center gap-1"><CornerDownRight className="h-2.5 w-2.5"/> Replying to {replyingTo.name}</span>
              <span className="text-[10px] text-muted-foreground truncate">{replyingTo.content}</span>
            </div>
            <button type="button" onClick={() => setReplyingTo(null)} className="text-muted-foreground hover:text-foreground p-0.5 shrink-0"><X className="h-3 w-3"/></button>
          </div>
        )}
        <form onSubmit={handleSendComment} className="flex items-end gap-2 w-full">
          <div className="flex-1 relative">
            <Input
              placeholder="Write a comment..."
              value={newCommentText}
              onChange={(e) => setNewCommentText(e.target.value)}
              className="text-xs min-h-9 h-auto py-2 bg-transparent border-none pr-10 focus-visible:ring-0 shadow-none"
            />
            <Button 
              type="submit" 
              size="icon" 
              variant="ghost"
              className="absolute right-1 bottom-1 h-7 w-7 text-primary hover:bg-primary/10 hover:text-primary transition-colors rounded-md" 
              disabled={!newCommentText.trim()}
            >
              <Send className="h-3.5 w-3.5" />
            </Button>
          </div>
        </form>
      </div>

      <AlertDialog  open={!!deleteCommentId} onOpenChange={(open) => !open && setDeleteCommentId(null)}>
        <AlertDialogContent className="rounded-md">
          <AlertDialogHeader>
            <AlertDialogTitle>Delete comment</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to delete this comment? This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction 
              onClick={confirmDelete}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              Delete
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
