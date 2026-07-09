import React, { useCallback, useEffect, useMemo, useState } from "react";
import { observer } from "mobx-react-lite";
import { toJS } from "mobx";
import { UserAvatar } from "@/components/user-avatar";
import { Send, CornerDownRight, Trash2, X, Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { MentionInput } from "@/components/mention-input";
import { toast } from "sonner";
import { cn } from "@/lib/utils";
import { useStore } from "@/stores/root.store";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useSyncEngine } from "@/sync/sync-provider";
import { CommentMutations } from "@/mutations/comment.mutations";
import { api } from "@/lib/api-client";
import type { CommentRecord } from "@/types/projects";
import type { PagedResult } from "@/types/paged-result";
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

// Renders comment text — handles new @[workspaceMemberId] tokens and legacy @Name text.
const CommentContent = observer(function CommentContent({ content }: { content: string }) {
  const rootStore = useWorkspaceRootStore();

  const idTokenRe  = /@\[([a-f0-9-]{36})\]/g;
  const nameTokenRe = /@((?:\w+)(?:\s(?!\s*@)\w+)*)/g;

  const segments: { index: number; length: number; label: string }[] = [];
  let m: RegExpExecArray | null;

  while ((m = idTokenRe.exec(content)) !== null) {
    const member = rootStore.memberStore.getById(m[1]);
    segments.push({ index: m.index, length: m[0].length, label: member?.name ?? `user` });
  }
  while ((m = nameTokenRe.exec(content)) !== null) {
    if (segments.some(s => m!.index >= s.index && m!.index < s.index + s.length)) continue;
    segments.push({ index: m.index, length: m[0].length, label: m[1] });
  }
  segments.sort((a, b) => a.index - b.index);

  const parts: React.ReactNode[] = [];
  let last = 0;
  for (const seg of segments) {
    if (seg.index > last) parts.push(content.slice(last, seg.index));
    parts.push(
      <span key={seg.index} className="inline-flex items-center bg-primary/15 text-primary text-[10px] font-black px-2 py-0.5 rounded-md mx-0.5 align-middle border border-primary/20">
        @{seg.label}
      </span>
    );
    last = seg.index + seg.length;
  }
  if (last < content.length) parts.push(content.slice(last));
  return <>{parts}</>;
});

interface TaskCommentsProps {
  taskId: string;
  // Hide the internal "Comments" heading/divider when this is embedded somewhere that already
  // shows its own title for it (e.g. the rail panel header in TaskDetailCanvas) — otherwise the
  // label shows up twice, once on the rail tab/panel header and once again in here.
  hideHeading?: boolean;
}

function useTaskComments(taskId: string) {
  const rootStore = useWorkspaceRootStore();
  const [items, setItems] = useState<CommentRecord[]>([]);
  const [nextCursor, setNextCursor] = useState<string | null>(null);
  const [hasNextPage, setHasNextPage] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [isFetchingNextPage, setIsFetchingNextPage] = useState(false);

  useEffect(() => {
    if (!taskId) return;
    let cancelled = false;
    const context = `comments:${taskId}`;

    (async () => {
      setIsLoading(true);
      const cached = await rootStore.commentDB!.getAllByTask(taskId);
      if (cancelled) return;
      if (cached.length > 0) {
        for (const comment of cached) {
          rootStore.commentStore.upsert(comment);
        }
      }

      const alreadyFetched = await rootStore.fetchedContextDB!.hasFetched(context);
      if (alreadyFetched) {
        if (!cancelled) {
          setItems(rootStore.commentStore.getByTask(taskId));
          setIsLoading(false);
        }
        return;
      }

      try {
        const { data } = await api.get<PagedResult<CommentRecord>>(`/tasks/${taskId}/comments`, {
          headers: { "X-Workspace-Id": rootStore.workspaceId },
        });
        if (cancelled) return;
        for (const comment of data.items) {
          rootStore.commentStore.upsert(comment);
        }
        await rootStore.commentDB!.putMany(data.items);
        setItems(rootStore.commentStore.getByTask(taskId));
        setNextCursor(data.nextCursor);
        setHasNextPage(data.hasNextPage);
        await rootStore.fetchedContextDB!.markFetched(context);
      } catch (err) {
        console.error(`Failed to fetch comments for task ${taskId}:`, err);
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    })();

    return () => { cancelled = true; };
  }, [taskId, rootStore]);

  const fetchNextPage = useCallback(() => {
    if (!nextCursor || isFetchingNextPage) return;
    setIsFetchingNextPage(true);
    api.get<PagedResult<CommentRecord>>(`/tasks/${taskId}/comments`, {
      params: { cursor: nextCursor },
      headers: { "X-Workspace-Id": rootStore.workspaceId },
    })
      .then(async ({ data }) => {
        for (const comment of data.items) {
          rootStore.commentStore.upsert(comment);
        }
        await rootStore.commentDB!.putMany(data.items);
        setItems((prev) => [...prev, ...data.items]);
        setNextCursor(data.nextCursor);
        setHasNextPage(data.hasNextPage);
      })
      .catch((err) => console.error(`Failed to fetch more comments for task ${taskId}:`, err))
      .finally(() => setIsFetchingNextPage(false));
  }, [taskId, nextCursor, isFetchingNextPage, rootStore]);

  return { items, isLoading, isFetchingNextPage, hasNextPage, fetchNextPage };
}

export const TaskComments = observer(function TaskComments({ taskId, hideHeading }: Readonly<TaskCommentsProps>) {
  const rootStore = useWorkspaceRootStore();
  const currentUserId = useStore().currentUserId;
  const allMembers = rootStore.memberStore.all;
  const syncEngine = useSyncEngine();
  const commentMutations = useMemo(
    () => new CommentMutations(rootStore, syncEngine, currentUserId),
    [rootStore, syncEngine, currentUserId],
  );

  const { items: fetchedComments, isLoading, isFetchingNextPage, hasNextPage, fetchNextPage } = useTaskComments(taskId);
  useEffect(() => {
    if (!fetchedComments) return;
    for (const c of fetchedComments) {
      rootStore.commentStore.upsert(c);
    }
    rootStore.commentDB?.putMany(toJS(fetchedComments)).catch((err) => console.error("Failed to persist comments locally", err));
  }, [fetchedComments, rootStore]);

  const comments = rootStore.commentStore.getByTask(taskId);

  const [newCommentText, setNewCommentText] = useState("");
  const [replyingTo, setReplyingTo] = useState<{ id: string, name: string, content: string } | null>(null);
  const [deleteCommentId, setDeleteCommentId] = useState<string | null>(null);

  const handleSendComment = async (e?: React.FormEvent) => {
    e?.preventDefault();
    if (!newCommentText.trim()) return;
    try {
      await commentMutations.create({ taskId, content: newCommentText.trim(), parentCommentId: replyingTo?.id });
      setNewCommentText("");
      setReplyingTo(null);
    } catch {
      toast.error("Failed to add comment. Please try again.");
    }
  };

  const confirmDelete = () => {
    if (deleteCommentId) {
      commentMutations.delete(deleteCommentId).catch((err) => console.error("Failed to delete comment", err));
      setDeleteCommentId(null);
    }
  };

  return (
    <div className={cn("space-y-2", !hideHeading && "pt-3 border-t border-border/30")}>
      {!hideHeading && (
        <h3 className="font-mono text-[10px] uppercase tracking-widest text-muted-foreground/70">
          Comments
        </h3>
      )}

      <div className="space-y-2 max-h-75 overflow-y-auto pr-1 [&::-webkit-scrollbar]:w-1 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20 hover:[&::-webkit-scrollbar-thumb]:bg-muted-foreground/45 [&::-webkit-scrollbar-track]:bg-transparent">
        {/* Load more — top, since oldest-first order */}
        {hasNextPage && (
          <button
            type="button"
            onClick={fetchNextPage}
            disabled={isFetchingNextPage}
            className="w-full flex items-center justify-center gap-1.5 py-1 text-[9px] font-semibold text-muted-foreground/60 hover:text-foreground transition-colors"
          >
            {isFetchingNextPage ? (
              <Loader2 className="h-3 w-3 animate-spin" />
            ) : (
              "Load earlier comments"
            )}
          </button>
        )}

        {isLoading ? (
          <div className="flex items-center justify-center py-4">
            <Loader2 className="h-4 w-4 animate-spin text-muted-foreground/40" />
          </div>
        ) : comments.length === 0 ? (
          <p className="text-[11px] text-muted-foreground/50 italic py-2">No comments yet. Start the conversation!</p>
        ) : (
          comments.map((comment) => {
            const creator = allMembers.find((m) => m.id === comment.creatorId);
            const name = creator?.name || "Unknown User";
            const parentComment = comment.parentCommentId ? comments.find(c => c.id === comment.parentCommentId) : null;
            const parentMember = parentComment ? allMembers.find(m => m.id === parentComment.creatorId) : null;
            const parentName = parentMember ? parentMember.name : "Unknown User";

            return (
              <div key={comment.id} className="flex gap-1.5 group">
                <UserAvatar
                  name={name}
                  avatarUrl={creator?.avatarUrl || null}
                  className="h-5 w-5 mt-0.5 shrink-0 rounded-sm"
                  fallbackClassName="text-[8px] font-bold rounded-sm"
                />
                <div className="flex flex-col gap-0.5 min-w-0 flex-1">
                  <div className="flex items-center gap-1.5">
                    <span className="font-semibold text-foreground text-[10px]">{name}</span>
                    <span className="text-[9px] text-muted-foreground/60 font-medium">
                      {new Date(comment.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                    </span>
                    <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity ml-auto shrink-0">
                      <button
                        type="button"
                        onClick={() => setReplyingTo({ id: comment.id, name, content: comment.content })}
                        className="text-muted-foreground hover:text-foreground transition-colors"
                        title="Reply"
                      >
                        <CornerDownRight className="h-2.5 w-2.5" />
                      </button>
                      <button
                        type="button"
                        onClick={() => setDeleteCommentId(comment.id)}
                        className="text-muted-foreground hover:text-destructive transition-colors"
                        title="Delete"
                      >
                        <Trash2 className="h-2.5 w-2.5" />
                      </button>
                    </div>
                  </div>

                  {parentComment && (
                    <div className="flex flex-col gap-0.5 pl-2 py-1 border-l-2 border-primary/30 bg-muted/20 rounded-r-md">
                      <span className="text-[8px] font-bold text-primary/80 flex items-center gap-1">
                        <CornerDownRight className="h-2 w-2" /> {parentName}
                      </span>
                      <span className="text-[9px] text-muted-foreground truncate opacity-80 italic line-clamp-1 whitespace-normal">{parentComment.content}</span>
                    </div>
                  )}

                  <div className="bg-muted/30 rounded-md px-2 py-1 text-[11px] text-foreground/90 leading-snug border border-border/5">
                    <CommentContent content={comment.content} />
                  </div>
                </div>
              </div>
            );
          })
        )}
      </div>

      <div className="mt-1 relative bg-muted/10 rounded-md border border-border/40 p-1 flex flex-col gap-1 shadow-sm" style={{ position: "relative" }}>
        {replyingTo && (
          <div className="flex items-center justify-between px-1.5 py-1 bg-muted/40 rounded-md border border-border/20 mx-0.5 mt-0.5">
            <div className="flex flex-col gap-0.5 overflow-hidden">
              <span className="text-[9px] font-bold text-primary flex items-center gap-1"><CornerDownRight className="h-2.5 w-2.5"/> Replying to {replyingTo.name}</span>
              <span className="text-[9px] text-muted-foreground truncate">{replyingTo.content}</span>
            </div>
            <button type="button" onClick={() => setReplyingTo(null)} className="text-muted-foreground hover:text-foreground p-0.5 shrink-0"><X className="h-3 w-3"/></button>
          </div>
        )}
        <div className="flex items-end gap-2 w-full">
          <MentionInput
            value={newCommentText}
            onChange={setNewCommentText}
            onSubmit={handleSendComment}
            placeholder="Write a comment... (@mention)"
            className="min-h-8 py-1.5 pr-10 text-[11px]"
          />
          <Button
            type="button"
            size="icon"
            variant="ghost"
            onClick={handleSendComment}
            className="absolute right-1 bottom-1 h-6 w-6 text-primary hover:bg-primary/10 hover:text-primary transition-colors rounded-md"
            disabled={!newCommentText.trim()}
          >
            <Send className="h-3 w-3" />
          </Button>
        </div>
      </div>

      <AlertDialog open={!!deleteCommentId} onOpenChange={(open) => !open && setDeleteCommentId(null)}>
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
});
