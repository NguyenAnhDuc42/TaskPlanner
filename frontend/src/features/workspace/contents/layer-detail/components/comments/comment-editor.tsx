import { useEditor, EditorContent } from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";
import Placeholder from "@tiptap/extension-placeholder";
import TaskList from "@tiptap/extension-task-list";
import TaskItem from "@tiptap/extension-task-item";
import CodeBlockLowlight from "@tiptap/extension-code-block-lowlight";
import { common, createLowlight } from "lowlight";
import { SlashCommand, getSuggestionItems, renderSuggestion } from "@/components/blockbase/extensions/slash-command";
import { Button } from "@/components/ui/button";
import { SendHorizontal } from "lucide-react";
const lowlight = createLowlight(common);

interface CommentEditorProps {
  onSubmit: (content: string) => void;
  isSubmitting?: boolean;
}

export function CommentEditor({ onSubmit, isSubmitting }: CommentEditorProps) {
  const editor = useEditor({
    extensions: [
      StarterKit.configure({
        codeBlock: false,
      }),
      Placeholder.configure({
        placeholder: "Write a comment or type '/' for commands...",
        emptyEditorClass:
          "cursor-text before:content-[attr(data-placeholder)] before:absolute before:text-muted-foreground/40 before:pointer-events-none before:not-italic",
      }),
      TaskList,
      TaskItem.configure({ nested: true }),
      CodeBlockLowlight.configure({ lowlight }),
      SlashCommand.configure({
        suggestion: {
          items: getSuggestionItems,
          render: renderSuggestion,
        },
      }),
    ],
    content: "",
    editorProps: {
      attributes: {
        class: "prose prose-sm dark:prose-invert focus:outline-none min-h-[80px] max-w-none text-[14px] leading-relaxed text-foreground/80 selection:bg-primary/20",
      },
      handleKeyDown: (view, event) => {
        // Shift+Enter creates new line, Enter alone submits (if you want that behavior)
        // But for rich text with blocks, Enter is needed for paragraphs.
        // Let's use Mod+Enter (Cmd/Ctrl + Enter) to submit.
        if (event.key === 'Enter' && (event.metaKey || event.ctrlKey)) {
          event.preventDefault();
          const json = view.state.doc.toJSON();
          onSubmit(JSON.stringify(json));
          return true;
        }
        return false;
      }
    },
  });

  // Clear editor when submitting completes successfully (you could pass a trigger for this, but for now we'll do it manually)
  const handleSubmit = () => {
    if (!editor) return;
    const json = editor.getJSON();
    // Don't submit if empty
    if (editor.getText().trim() === "" && !json.content?.some((n: any) => n.type !== 'paragraph' || n.content)) return;
    
    onSubmit(JSON.stringify(json));
    editor.commands.clearContent();
  };

  return (
    <div className="relative flex flex-col w-full border border-border/10 bg-[#0f0f12] rounded-xl overflow-hidden focus-within:border-primary/30 focus-within:ring-1 focus-within:ring-primary/20 transition-all shadow-sm">
      <div className="flex-1 p-3 cursor-text block-editor-styles max-h-[400px] overflow-y-auto no-scrollbar">
        <EditorContent editor={editor} />
      </div>
      
      <div className="flex items-center justify-between px-3 py-2 bg-muted/5 border-t border-border/5">
        <div className="text-[10px] text-muted-foreground/40 font-medium">
          <span className="inline-block px-1.5 py-0.5 rounded bg-muted/20 border border-border/10 mr-1">⌘</span>
          <span className="inline-block px-1.5 py-0.5 rounded bg-muted/20 border border-border/10 mr-2">Enter</span>
          to submit
        </div>
        
        <Button 
          size="sm" 
          onClick={handleSubmit} 
          disabled={isSubmitting || editor?.isEmpty}
          className="h-7 px-3 text-[11px] font-bold tracking-wide rounded-md"
        >
          {isSubmitting ? "Sending..." : "Comment"}
          <SendHorizontal className="ml-1.5 h-3 w-3" />
        </Button>
      </div>
    </div>
  );
}
