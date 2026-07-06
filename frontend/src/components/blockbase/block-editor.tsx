import "@blocknote/mantine/style.css";
import { useRef } from "react";
import { filterSuggestionItems, type PartialBlock, type Block } from "@blocknote/core";
import { useCreateBlockNote, SuggestionMenuController, getDefaultReactSlashMenuItems } from "@blocknote/react";
import { BlockNoteView } from "@blocknote/mantine";
import { useBlockEditorSync } from "@/features/workspace/contents/views/view-components/use-block-editor-sync";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { api } from "@/lib/api-client";

const BLOCKED_SLASH_ITEMS = new Set(["Audio"]);

interface BlockEditorProps {
  documentId: string;
  editable?: boolean;
}

function EditorView({ initialContent, onUpdate, editable = true }: {
  initialContent: PartialBlock[] | undefined;
  onUpdate: (blocks: Block[]) => void;
  editable?: boolean;
}) {
  const { workspaceId } = useWorkspaceRootStore();

  const editor = useCreateBlockNote({
    initialContent: initialContent as never,
    uploadFile: async (file: File) => {
      const formData = new FormData();
      formData.append("file", file);
      const { data } = await api.post<{ url: string }>("/attachments/sync/upload", formData, {

        headers: { "X-Workspace-Id": workspaceId, "Content-Type": undefined },
      });
      return data.url;
    },
  });

  const isDark = document.documentElement.classList.contains("dark");
  const skipFirstChange = useRef(true);

  return (
    // max ~150 blocks worth of height then scroll within editor
    <div className="max-h-900 overflow-y-auto [&::-webkit-scrollbar]:w-1 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20 hover:[&::-webkit-scrollbar-thumb]:bg-muted-foreground/40 [&::-webkit-scrollbar-track]:bg-transparent">
      <BlockNoteView
        editor={editor}
        theme={isDark ? "dark" : "light"}
        editable={editable}
        onChange={() => {
          if (skipFirstChange.current) { skipFirstChange.current = false; return; }
          if (editable) onUpdate(editor.document as unknown as Block[]);
        }}
        sideMenu={false}
        slashMenu={false}
      >
        <SuggestionMenuController
          triggerCharacter="/"
          getItems={async (query: string) =>
            filterSuggestionItems(
              getDefaultReactSlashMenuItems(editor).filter((item: { title: string }) => !BLOCKED_SLASH_ITEMS.has(item.title)),
              query
            )
          }
        />
      </BlockNoteView>
    </div>
  );
}

export function BlockEditor({ documentId, editable = true }: Readonly<BlockEditorProps>) {
  const { initialContent, handleUpdate, isReady, version } = useBlockEditorSync(documentId);

  if (!isReady) {
    return <div className="py-2 text-xs text-muted-foreground/50 animate-pulse">Loading…</div>;
  }

  return (
    <EditorView
      key={`${documentId}-${version}`}
      initialContent={initialContent as PartialBlock[] | undefined}
      onUpdate={handleUpdate}
      editable={editable}
    />
  );
}
