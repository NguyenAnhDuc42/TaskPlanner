import { useEditor, EditorContent } from "@tiptap/react";
import { useEffect, useRef, useCallback, useState, memo } from "react";
import { BubbleMenu } from "@tiptap/react/menus";
import StarterKit from "@tiptap/starter-kit";
import Placeholder from "@tiptap/extension-placeholder";
import TaskList from "@tiptap/extension-task-list";
import TaskItem from "@tiptap/extension-task-item";
import { Bold, Italic, Strikethrough, Heading1, Heading2, Code, Quote } from "lucide-react";
import { cn } from "@/lib/utils";

import { useBlockEditorSync } from "@/features/workspace/contents/views/view-components/use-block-editor-sync";
import { IdExtension } from "./extensions/id-extension";
import { SlashCommand, getSuggestionItems, renderSuggestion } from "./extensions/slash-command";
import CodeBlockLowlight from "@tiptap/extension-code-block-lowlight";
import { common, createLowlight } from "lowlight";

const lowlight = createLowlight(common);

interface BlockEditorProps {
  readonly documentId: string;
  readonly placeholder?: string;
}

// ============================================================================
// STYLES — injected once at module load
// ============================================================================

const EDITOR_STYLES = `
  /* ── Base typography ─────────────────────────────────────────────────────── */
  .be .tiptap {
    outline: none;
    color: hsl(var(--foreground) / 0.82);
    font-size: 15px;
    line-height: 1.7;
    font-family: inherit;
  }

  /* ── Placeholder ─────────────────────────────────────────────────────────── */
  .be .tiptap p.is-editor-empty:first-child::before {
    content: attr(data-placeholder);
    float: left;
    color: hsl(var(--muted-foreground) / 0.3);
    pointer-events: none;
    height: 0;
    font-style: normal;
  }

  /* ── Paragraphs ─────────────────────────────────────────────────────────── */
  .be .tiptap p {
    margin: 2px 0;
    padding: 2px 4px;
    border-radius: 4px;
    transition: background 0.1s;
    min-height: 1.7em;
  }
  .be .tiptap p:hover {
    background: hsl(var(--foreground) / 0.025);
  }

  /* ── Headings ─────────────────────────────────────────────────────────── */
  .be .tiptap h1 {
    font-size: 2em;
    font-weight: 800;
    letter-spacing: -0.04em;
    line-height: 1.2;
    margin: 1.4em 0 0.3em;
    padding: 2px 4px;
    border-radius: 4px;
    color: hsl(var(--foreground) / 0.95);
  }
  .be .tiptap h2 {
    font-size: 1.45em;
    font-weight: 700;
    letter-spacing: -0.025em;
    line-height: 1.3;
    margin: 1.1em 0 0.25em;
    padding: 2px 4px;
    border-radius: 4px;
    color: hsl(var(--foreground) / 0.92);
  }
  .be .tiptap h3 {
    font-size: 1.15em;
    font-weight: 700;
    letter-spacing: -0.015em;
    line-height: 1.35;
    margin: 0.9em 0 0.2em;
    padding: 2px 4px;
    border-radius: 4px;
    color: hsl(var(--foreground) / 0.88);
  }

  /* ── Lists ─────────────────────────────────────────────────────────── */
  .be .tiptap ul:not([data-type="taskList"]) {
    list-style: disc;
    padding-left: 1.5em;
    margin: 4px 0;
  }
  .be .tiptap ol {
    list-style: decimal;
    padding-left: 1.5em;
    margin: 4px 0;
  }
  .be .tiptap li {
    margin: 1px 0;
    padding: 1px 0;
  }
  .be .tiptap ul[data-type="taskList"] {
    list-style: none;
    padding-left: 0;
    margin: 4px 0;
  }
  .be .tiptap li[data-type="taskItem"] {
    display: flex !important;
    flex-direction: row !important;
    align-items: baseline;
    gap: 8px;
    margin: 2px 0;
    padding: 2px 4px;
    border-radius: 4px;
    transition: background 0.1s;
  }
  .be .tiptap li[data-type="taskItem"]:hover {
    background: hsl(var(--foreground) / 0.025);
  }
  .be .tiptap li[data-type="taskItem"] > label {
    display: flex !important;
    align-items: center;
    flex-shrink: 0;
    margin-top: 0;
    line-height: 1;
    cursor: pointer;
  }
  .be .tiptap li[data-type="taskItem"] > div {
    flex: 1;
    min-width: 0;
  }
  .be .tiptap li[data-type="taskItem"] input[type="checkbox"] {
    appearance: none;
    -webkit-appearance: none;
    height: 15px;
    width: 15px;
    border-radius: 4px;
    border: 1.5px solid hsl(var(--border) / 0.7);
    background: transparent;
    cursor: pointer;
    position: relative;
    transition: all 0.15s;
  }
  .be .tiptap li[data-type="taskItem"] input[type="checkbox"]:checked {
    background: hsl(var(--primary));
    border-color: hsl(var(--primary));
  }
  .be .tiptap li[data-type="taskItem"] input[type="checkbox"]:checked::after {
    content: '';
    position: absolute;
    left: 3px;
    top: 1px;
    width: 5px;
    height: 9px;
    border-right: 2px solid white;
    border-bottom: 2px solid white;
    transform: rotate(45deg);
    display: block;
  }
  .be .tiptap li[data-type="taskItem"][data-checked="true"] > div p {
    color: hsl(var(--muted-foreground) / 0.5);
    text-decoration: line-through;
    text-decoration-color: hsl(var(--muted-foreground) / 0.3);
  }

  /* ── Blockquote ─────────────────────────────────────────────────────────── */
  .be .tiptap blockquote {
    border-left: 3px solid hsl(var(--primary) / 0.5);
    padding: 6px 12px;
    margin: 8px 0;
    border-radius: 0 6px 6px 0;
    background: hsl(var(--primary) / 0.04);
    color: hsl(var(--foreground) / 0.75);
    font-style: italic;
  }

  /* ── Code Inline ─────────────────────────────────────────────────────────── */
  .be .tiptap code:not(pre code) {
    font-family: 'JetBrains Mono Variable', 'Menlo', monospace;
    font-size: 0.85em;
    background: hsl(var(--muted) / 0.7);
    border: 1px solid hsl(var(--border) / 0.4);
    border-radius: 4px;
    padding: 1px 5px;
    color: hsl(var(--foreground) / 0.85);
  }

  /* ── Code Block ─────────────────────────────────────────────────────────── */
  .be .tiptap pre {
    background: hsl(0 0% 5%);
    border: 1px solid hsl(var(--border) / 0.2);
    border-radius: 10px;
    padding: 16px 20px;
    margin: 10px 0;
    overflow-x: auto;
    font-family: 'JetBrains Mono Variable', 'Menlo', monospace;
    font-size: 13px;
    line-height: 1.6;
  }
  .be .tiptap pre code {
    background: transparent;
    padding: 0;
    border: none;
    font-size: inherit;
    color: hsl(var(--foreground) / 0.88);
  }

  /* ── Horizontal Rule ─────────────────────────────────────────────────────── */
  .be .tiptap hr {
    border: none;
    border-top: 1px solid hsl(var(--border) / 0.3);
    margin: 20px 0;
    height: 0;
  }

  /* ── Strong / Em ─────────────────────────────────────────────────────────── */
  .be .tiptap strong {
    font-weight: 700;
    color: hsl(var(--foreground) / 0.95);
  }
  .be .tiptap em {
    font-style: italic;
    color: hsl(var(--foreground) / 0.8);
  }
  .be .tiptap s {
    text-decoration: line-through;
    color: hsl(var(--muted-foreground) / 0.6);
  }

  /* ── Selection ─────────────────────────────────────────────────────────── */
  .be .tiptap ::selection {
    background: hsl(var(--primary) / 0.18);
  }
`;

if (typeof document !== "undefined" && !document.getElementById("block-editor-styles-v3")) {
  // Remove old style tags to prevent conflicts
  ["block-editor-styles-v2", "block-editor-styles"].forEach((id) => {
    document.getElementById(id)?.remove();
  });
  const styleEl = document.createElement("style");
  styleEl.id = "block-editor-styles-v3";
  styleEl.textContent = EDITOR_STYLES;
  document.head.appendChild(styleEl);
}

// ============================================================================
// BUBBLE MENU — context toolbar on text selection
// ============================================================================

const btnClass = (active: boolean) =>
  cn(
    "p-1.5 rounded-md transition-all text-[13px] font-semibold leading-none",
    active
      ? "bg-white/20 text-white"
      : "text-white/70 hover:bg-white/10 hover:text-white"
  );

const BubbleToolbar = memo(function BubbleToolbar({ editor }: Readonly<{ editor: any }>) {
  return (
    <BubbleMenu editor={editor} options={{ duration: 100 } as any}>
      <div className="flex items-center gap-0.5 p-1 bg-foreground/90 rounded-lg shadow-xl backdrop-blur-md animate-in fade-in slide-in-from-bottom-1 duration-150">
        <button
          type="button"
          onClick={() => editor.chain().focus().toggleBold().run()}
          className={btnClass(editor.isActive("bold"))}
          title="Bold"
        >
          <Bold className="h-3.5 w-3.5" />
        </button>
        <button
          type="button"
          onClick={() => editor.chain().focus().toggleItalic().run()}
          className={btnClass(editor.isActive("italic"))}
          title="Italic"
        >
          <Italic className="h-3.5 w-3.5" />
        </button>
        <button
          type="button"
          onClick={() => editor.chain().focus().toggleStrike().run()}
          className={btnClass(editor.isActive("strike"))}
          title="Strikethrough"
        >
          <Strikethrough className="h-3.5 w-3.5" />
        </button>
        <button
          type="button"
          onClick={() => editor.chain().focus().toggleCode().run()}
          className={btnClass(editor.isActive("code"))}
          title="Inline code"
        >
          <Code className="h-3.5 w-3.5" />
        </button>

        <div className="w-px h-3.5 bg-white/15 mx-0.5" />

        <button
          type="button"
          onClick={() => editor.chain().focus().toggleHeading({ level: 1 }).run()}
          className={btnClass(editor.isActive("heading", { level: 1 }))}
          title="Heading 1"
        >
          <Heading1 className="h-3.5 w-3.5" />
        </button>
        <button
          type="button"
          onClick={() => editor.chain().focus().toggleHeading({ level: 2 }).run()}
          className={btnClass(editor.isActive("heading", { level: 2 }))}
          title="Heading 2"
        >
          <Heading2 className="h-3.5 w-3.5" />
        </button>
        <button
          type="button"
          onClick={() => editor.chain().focus().toggleBlockquote().run()}
          className={btnClass(editor.isActive("blockquote"))}
          title="Quote"
        >
          <Quote className="h-3.5 w-3.5" />
        </button>
      </div>
    </BubbleMenu>
  );
});

// ============================================================================
// SAVE INDICATOR
// ============================================================================

type SaveState = "saved" | "saving" | "unsaved";

const SaveIndicator = memo(function SaveIndicator({ state }: Readonly<{ state: SaveState }>) {
  return (
    <div className="flex items-center gap-1.5 text-[10px] font-medium text-muted-foreground/40 select-none transition-all duration-300">
      {state === "saving" && (
        <>
          <span className="h-1.5 w-1.5 rounded-full bg-amber-500/70 animate-pulse" />
          <span>Saving…</span>
        </>
      )}
      {state === "saved" && (
        <>
          <span className="h-1.5 w-1.5 rounded-full bg-emerald-500/60" />
          <span>Saved</span>
        </>
      )}
      {state === "unsaved" && (
        <>
          <span className="h-1.5 w-1.5 rounded-full bg-muted-foreground/30" />
          <span>Unsaved changes</span>
        </>
      )}
    </div>
  );
});

// ============================================================================
// MAIN EDITOR COMPONENT
// ============================================================================

export function BlockEditor({ documentId, placeholder }: Readonly<BlockEditorProps>) {
  const { initialContent, handleUpdate } = useBlockEditorSync(documentId);
  const isSettingContent = useRef(false);
  const updateTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const [saveState, setSaveState] = useState<SaveState>("saved");

  const debouncedHandleUpdate = useCallback(
    (content: any) => {
      setSaveState("unsaved");
      if (updateTimeoutRef.current) clearTimeout(updateTimeoutRef.current);
      updateTimeoutRef.current = setTimeout(() => {
        setSaveState("saving");
        try {
          handleUpdate(content);
        } finally {
          setSaveState("saved");
        }
      }, 800);
    },
    [handleUpdate]
  );

  const editor = useEditor({
    extensions: [
      StarterKit.configure({ codeBlock: false }),
      IdExtension,
      Placeholder.configure({
        placeholder: placeholder || "Write something, or type '/' for commands…",
        emptyEditorClass: "is-editor-empty",
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
        class: "focus:outline-none min-h-[300px] max-w-none",
        spellcheck: "true",
      },
    },
    onUpdate: ({ editor }) => {
      if (isSettingContent.current) return;
      debouncedHandleUpdate(editor.getJSON());
    },
  });

  // Initialize content once after data loads
  const isInitialized = useRef(false);
  useEffect(() => {
    if (editor && initialContent.content.length > 0 && !isInitialized.current) {
      isSettingContent.current = true;
      editor.commands.setContent(initialContent);
      isSettingContent.current = false;
      isInitialized.current = true;
    }
  }, [editor, initialContent]);

  // Cleanup debounce on unmount
  useEffect(() => {
    return () => {
      if (updateTimeoutRef.current) clearTimeout(updateTimeoutRef.current);
    };
  }, []);

  if (!editor) return null;

  return (
    <div className="relative flex flex-col h-full">
      {/* Toolbar row */}
      <div className="flex items-center justify-between px-1 py-1 mb-1">
        <div className="flex items-center gap-0.5">
          {[
            { icon: Bold, cmd: () => editor.chain().focus().toggleBold().run(), active: editor.isActive("bold"), title: "Bold" },
            { icon: Italic, cmd: () => editor.chain().focus().toggleItalic().run(), active: editor.isActive("italic"), title: "Italic" },
            { icon: Strikethrough, cmd: () => editor.chain().focus().toggleStrike().run(), active: editor.isActive("strike"), title: "Strikethrough" },
            { icon: Code, cmd: () => editor.chain().focus().toggleCode().run(), active: editor.isActive("code"), title: "Code" },
          ].map(({ icon: Icon, cmd, active, title }) => (
            <button
              key={title}
              type="button"
              onClick={cmd}
              title={title}
              className={cn(
                "p-1.5 rounded-md transition-all",
                active
                  ? "bg-primary/10 text-primary"
                  : "text-muted-foreground/40 hover:text-foreground hover:bg-muted/60"
              )}
            >
              <Icon className="h-3.5 w-3.5" />
            </button>
          ))}

          <div className="w-px h-3.5 bg-border/30 mx-1" />

          {[
            { icon: Heading1, cmd: () => editor.chain().focus().toggleHeading({ level: 1 }).run(), active: editor.isActive("heading", { level: 1 }), title: "H1" },
            { icon: Heading2, cmd: () => editor.chain().focus().toggleHeading({ level: 2 }).run(), active: editor.isActive("heading", { level: 2 }), title: "H2" },
            { icon: Quote, cmd: () => editor.chain().focus().toggleBlockquote().run(), active: editor.isActive("blockquote"), title: "Quote" },
          ].map(({ icon: Icon, cmd, active, title }) => (
            <button
              key={title}
              type="button"
              onClick={cmd}
              title={title}
              className={cn(
                "p-1.5 rounded-md transition-all",
                active
                  ? "bg-primary/10 text-primary"
                  : "text-muted-foreground/40 hover:text-foreground hover:bg-muted/60"
              )}
            >
              <Icon className="h-3.5 w-3.5" />
            </button>
          ))}
        </div>

        <SaveIndicator state={saveState} />
      </div>

      {/* Bubble menu on selection */}
      <BubbleToolbar editor={editor} />

      {/* Editor surface */}
      <div className="flex-1 cursor-text be">
        <EditorContent editor={editor} />
      </div>

      {/* Slash hint */}
      <div className="pt-4 pb-1 px-1 text-[10px] text-muted-foreground/25 select-none">
        Type <kbd className="font-mono bg-muted/40 px-1 rounded text-[9px] border border-border/20">/</kbd> to insert blocks
      </div>
    </div>
  );
}
