import { useEditor, EditorContent } from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";
import Placeholder from "@tiptap/extension-placeholder";
import { Bold, Italic, Strikethrough, List, ListOrdered } from "lucide-react";
import { cn } from "@/lib/utils";

interface RichTextEditorProps {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
}

export function RichTextEditor({ value, onChange, placeholder }: RichTextEditorProps) {
  const editor = useEditor({
    extensions: [
      StarterKit,
      Placeholder.configure({
        placeholder: placeholder || "Write something...",
        emptyEditorClass:
          "cursor-text before:content-[attr(data-placeholder)] before:absolute before:text-muted-foreground/40 before:pointer-events-none before:italic",
      }),
    ],
    content: value,
    editorProps: {
      attributes: {
        class: "prose prose-sm dark:prose-invert focus:outline-none min-h-[300px] max-w-none text-[15px] leading-relaxed text-foreground/80",
      },
    },
    onUpdate: ({ editor }) => {
      onChange(editor.getHTML());
    },
  });

  if (!editor) return null;

  return (
    <div className="flex flex-col border border-border/40 rounded-xl overflow-hidden bg-background/50 focus-within:border-primary/30 focus-within:shadow-[0_0_0_1px_rgba(var(--primary),0.1)] transition-all">
      {/* Toolbar */}
      <div className="flex items-center gap-1 p-2 border-b border-border/40 bg-muted/20">
        <button
          onClick={() => editor.chain().focus().toggleBold().run()}
          className={cn(
            "p-1.5 rounded-lg text-muted-foreground transition-colors",
            editor.isActive("bold") ? "bg-primary/10 text-primary" : "hover:bg-muted"
          )}
        >
          <Bold className="h-4 w-4" />
        </button>
        <button
          onClick={() => editor.chain().focus().toggleItalic().run()}
          className={cn(
            "p-1.5 rounded-lg text-muted-foreground transition-colors",
            editor.isActive("italic") ? "bg-primary/10 text-primary" : "hover:bg-muted"
          )}
        >
          <Italic className="h-4 w-4" />
        </button>
        <button
          onClick={() => editor.chain().focus().toggleStrike().run()}
          className={cn(
            "p-1.5 rounded-lg text-muted-foreground transition-colors",
            editor.isActive("strike") ? "bg-primary/10 text-primary" : "hover:bg-muted"
          )}
        >
          <Strikethrough className="h-4 w-4" />
        </button>
        
        <div className="w-px h-4 bg-border/50 mx-1" />

        <button
          onClick={() => editor.chain().focus().toggleBulletList().run()}
          className={cn(
            "p-1.5 rounded-lg text-muted-foreground transition-colors",
            editor.isActive("bulletList") ? "bg-primary/10 text-primary" : "hover:bg-muted"
          )}
        >
          <List className="h-4 w-4" />
        </button>
        <button
          onClick={() => editor.chain().focus().toggleOrderedList().run()}
          className={cn(
            "p-1.5 rounded-lg text-muted-foreground transition-colors",
            editor.isActive("orderedList") ? "bg-primary/10 text-primary" : "hover:bg-muted"
          )}
        >
          <ListOrdered className="h-4 w-4" />
        </button>
      </div>

      {/* Editor Content */}
      <div className="p-4 cursor-text bg-transparent">
        <EditorContent editor={editor} />
      </div>
    </div>
  );
}
