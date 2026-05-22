import { useEditor, EditorContent } from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";
import TaskList from "@tiptap/extension-task-list";
import TaskItem from "@tiptap/extension-task-item";
import CodeBlockLowlight from "@tiptap/extension-code-block-lowlight";
import { common, createLowlight } from "lowlight";
import { useTaskComments, useAddComment } from "../../views/task/task-api";
import { CommentEditor } from "./comment-editor";
import { useWorkspace } from "@/features/workspace/context/workspace-provider";
import { formatDistanceToNow } from "date-fns";
import { useMemo, useEffect } from "react";
import { Loader2 } from "lucide-react";

const lowlight = createLowlight(common);

function CommentViewer({ content }: { content: string }) {
  const contentObj = useMemo(() => {
    try {
      return JSON.parse(content);
    } catch {
      return { type: "doc", content: [{ type: "paragraph", content: [{ type: "text", text: content }] }] };
    }
  }, [content]);

  const editor = useEditor({
    editable: false,
    extensions: [
      StarterKit.configure({
        codeBlock: false,
      }),
      TaskList,
      TaskItem.configure({ nested: true }),
      CodeBlockLowlight.configure({ lowlight }),
    ],
    content: contentObj,
    editorProps: {
      attributes: {
        class: "prose prose-sm dark:prose-invert max-w-none text-[13.5px] leading-relaxed text-foreground/85",
      },
    },
  });

  // Re-sync if content changes
  useEffect(() => {
    if (editor && contentObj && editor.getJSON() !== contentObj) {
      editor.commands.setContent(contentObj);
    }
  }, [editor, contentObj]);

  if (!editor) return null;

  return (
    <div className="block-editor-styles">
      <EditorContent editor={editor} />
    </div>
  );
}

export function CommentSection({ taskId }: { taskId: string }) {
  const { data: comments, isLoading } = useTaskComments(taskId);
  const addComment = useAddComment();
  const { registry } = useWorkspace();

  const handleAddComment = (content: string) => {
    addComment.mutate({ taskId, content });
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3 mb-6">
        <h3 className="text-[11px] font-black uppercase tracking-[0.2em] text-muted-foreground/40">Comments</h3>
        <div className="h-px flex-1 bg-border/5" />
      </div>

      <div className="space-y-6">
        {isLoading ? (
          <div className="flex justify-center p-4">
            <Loader2 className="h-4 w-4 animate-spin text-muted-foreground/40" />
          </div>
        ) : comments?.length === 0 ? (
          <div className="text-[12px] text-muted-foreground/40 italic pl-1">
            No comments yet. Start the conversation.
          </div>
        ) : (
          <div className="space-y-6">
            {comments?.map((comment) => {
              const creator = registry.memberMap[comment.creatorId];
              return (
                <div key={comment.id} className="flex gap-4">
                  <div className="h-7 w-7 rounded-full bg-muted flex items-center justify-center text-[10px] font-bold uppercase overflow-hidden shrink-0 mt-0.5">
                    {creator?.avatarUrl ? (
                      <img src={creator.avatarUrl} alt={creator.name} className="h-full w-full object-cover" />
                    ) : (
                      <span>{creator?.name?.[0] || "?"}</span>
                    )}
                  </div>
                  
                  <div className="flex-1 min-w-0">
                    <div className="flex items-baseline gap-2 mb-1">
                      <span className="text-[13px] font-bold text-foreground/90">{creator?.name || "Unknown"}</span>
                      <span className="text-[11px] text-muted-foreground/50">
                        {formatDistanceToNow(new Date(comment.createdAt), { addSuffix: true })}
                      </span>
                    </div>
                    
                    <div className="pl-0">
                      <CommentViewer content={comment.content} />
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>

      <div className="pt-2">
        <CommentEditor onSubmit={handleAddComment} isSubmitting={addComment.isPending} />
      </div>
    </div>
  );
}
