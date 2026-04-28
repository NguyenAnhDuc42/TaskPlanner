import { useEditor, EditorContent } from "@tiptap/react";
import { BubbleMenu, FloatingMenu } from "@tiptap/react/menus";
import StarterKit from "@tiptap/starter-kit";
import Placeholder from "@tiptap/extension-placeholder";
import TaskList from "@tiptap/extension-task-list";
import TaskItem from "@tiptap/extension-task-item";
import { 
  Bold, 
  Italic, 
  Strikethrough, 
  List, 
  Heading1, 
  Heading2, 
  CheckSquare
} from "lucide-react";
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
        placeholder: placeholder || "Write something or type '/'...",
        emptyEditorClass:
          "cursor-text before:content-[attr(data-placeholder)] before:absolute before:text-muted-foreground/20 before:pointer-events-none before:not-italic",
      }),
      TaskList,
      TaskItem.configure({
        nested: true,
      }),
    ],
    content: value,
    editorProps: {
      attributes: {
        class: "prose prose-sm dark:prose-invert focus:outline-none min-h-[400px] max-w-none text-[15px] leading-relaxed text-foreground/70 selection:bg-primary/20",
      },
    },
    onUpdate: ({ editor }) => {
      onChange(editor.getHTML());
    },
  });

  if (!editor) return null;

  return (
    <div className="relative group/editor flex flex-col h-full">
      
      {/* FLOATING MENU - APPEARS ON EMPTY LINES (+) */}
      <FloatingMenu editor={editor} options={{ duration: 100 } as any}>
        <div className="flex items-center gap-1 p-1 bg-background border border-border/50 rounded-xl shadow-2xl animate-in fade-in zoom-in-95 duration-200">
          <MenuButton 
            onClick={() => editor.chain().focus().toggleHeading({ level: 1 }).run()}
            icon={Heading1}
            active={editor.isActive('heading', { level: 1 })}
          />
          <MenuButton 
            onClick={() => editor.chain().focus().toggleHeading({ level: 2 }).run()}
            icon={Heading2}
            active={editor.isActive('heading', { level: 2 })}
          />
          <MenuButton 
            onClick={() => editor.chain().focus().toggleBulletList().run()}
            icon={List}
            active={editor.isActive('bulletList')}
          />
          <MenuButton 
            onClick={() => editor.chain().focus().toggleTaskList().run()}
            icon={CheckSquare}
            active={editor.isActive('taskList')}
          />
        </div>
      </FloatingMenu>

      {/* BUBBLE MENU - APPEARS ON SELECTION */}
      <BubbleMenu editor={editor} options={{ duration: 100 } as any}>
        <div className="flex items-center gap-0.5 p-1 bg-foreground/90 text-background rounded-lg shadow-xl backdrop-blur-md animate-in fade-in slide-in-from-bottom-2 duration-200">
          <BubbleButton 
            onClick={() => editor.chain().focus().toggleBold().run()}
            icon={Bold}
            active={editor.isActive('bold')}
          />
          <BubbleButton 
            onClick={() => editor.chain().focus().toggleItalic().run()}
            icon={Italic}
            active={editor.isActive('italic')}
          />
          <BubbleButton 
            onClick={() => editor.chain().focus().toggleStrike().run()}
            icon={Strikethrough}
            active={editor.isActive('strike')}
          />
          <div className="w-px h-3 bg-background/20 mx-1" />
          <BubbleButton 
            onClick={() => editor.chain().focus().toggleHeading({ level: 1 }).run()}
            icon={Heading1}
            active={editor.isActive('heading', { level: 1 })}
          />
        </div>
      </BubbleMenu>

      {/* EDITOR CONTENT AREA */}
      <div className="flex-1 cursor-text block-editor-styles">
        <EditorContent editor={editor} />
      </div>

      <style dangerouslySetInnerHTML={{ __html: `
        .block-editor-styles .tiptap p {
          @apply my-1 transition-all duration-200 py-1 px-2 rounded-md hover:bg-foreground/[0.02];
        }
        .block-editor-styles .tiptap h1, .block-editor-styles .tiptap h2 {
          @apply mt-6 mb-2 px-2 transition-all duration-200 rounded-md hover:bg-foreground/[0.02];
        }
        .block-editor-styles .tiptap ul[data-type="taskList"] {
          @apply list-none p-0;
        }
        .block-editor-styles .tiptap li[data-type="taskItem"] {
          @apply flex items-start gap-2 mb-1 px-2 py-1 transition-all duration-200 rounded-md hover:bg-foreground/[0.02];
        }
        .block-editor-styles .tiptap li[data-type="taskItem"] > label {
          @apply mt-1.5 shrink-0;
        }
        .block-editor-styles .tiptap li[data-type="taskItem"] > div {
          @apply flex-1;
        }
        .block-editor-styles .tiptap li[data-type="taskItem"] input[type="checkbox"] {
          @apply appearance-none h-4 w-4 rounded border border-border/60 checked:bg-primary checked:border-primary transition-all cursor-pointer relative after:content-[''] after:hidden checked:after:block after:absolute after:left-1 after:top-0.5 after:w-1.5 after:h-2.5 after:border-r-2 after:border-b-2 after:border-white after:rotate-45;
        }
      `}} />
    </div>
  );
}

function MenuButton({ onClick, icon: Icon, active }: { onClick: () => void, icon: any, active?: boolean }) {
  return (
    <button
      onClick={onClick}
      className={cn(
        "p-1.5 rounded-lg text-muted-foreground transition-all hover:text-foreground hover:bg-muted",
        active && "text-primary bg-primary/10"
      )}
    >
      <Icon className="h-4 w-4" />
    </button>
  );
}

function BubbleButton({ onClick, icon: Icon, active }: { onClick: () => void, icon: any, active?: boolean }) {
  return (
    <button
      onClick={onClick}
      className={cn(
        "p-1.5 rounded-md transition-all hover:bg-background/10",
        active ? "text-primary" : "text-background"
      )}
    >
      <Icon className="h-3.5 w-3.5" />
    </button>
  );
}
