import "@blocknote/mantine/style.css";
import "./block-editor.css";
import { useRef } from "react";
import { BlockNoteSchema, defaultBlockSpecs, filterSuggestionItems, type PartialBlock, type Block } from "@blocknote/core";
import { createCodeBlockSpec } from "@blocknote/core/blocks";
import { codeBlockOptions } from "@blocknote/code-block";
import { useCreateBlockNote, SuggestionMenuController, getDefaultReactSlashMenuItems } from "@blocknote/react";
import { BlockNoteView } from "@blocknote/mantine";
import { useBlockEditorSync } from "@/features/workspace/contents/views/view-components/use-block-editor-sync";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { api } from "@/lib/api-client";

const BLOCKED_SLASH_ITEMS = new Set(["Audio"]);
const MEDIA_BLOCK_TYPES = new Set(["image", "video", "file"]);

const schema = BlockNoteSchema.create({
  blockSpecs: {
    ...defaultBlockSpecs,
    codeBlock: createCodeBlockSpec(codeBlockOptions),
  },
});

type AnyBlock = { id: string; type: string; props?: Record<string, unknown>; children?: AnyBlock[] };

function collectBlockIds(blocks: AnyBlock[], out = new Set<string>()): Set<string> {
  for (const block of blocks) {
    out.add(block.id);
    if (block.children?.length) collectBlockIds(block.children, out);
  }
  return out;
}

function collectMediaBlocks(blocks: AnyBlock[], out: AnyBlock[] = []): AnyBlock[] {
  for (const block of blocks) {
    if (MEDIA_BLOCK_TYPES.has(block.type)) out.push(block);
    if (block.children?.length) collectMediaBlocks(block.children, out);
  }
  return out;
}

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
    schema,
    initialContent: initialContent as never,
    uploadFile: async (file: File) => {
      const formData = new FormData();
      formData.append("file", file);
      const { data } = await api.post<{ url: string }>("/attachments/sync/upload", formData, {
        headers: { "X-Workspace-Id": workspaceId },
      });
      return data.url;
    },
  });

  const isDark = document.documentElement.classList.contains("dark");
  const skipFirstChange = useRef(true);
  const seenBlockIdsRef = useRef<Set<string> | null>(null);

  return (
    // max ~150 blocks worth of height then scroll within editor
    <div className="max-h-900 overflow-y-auto [&::-webkit-scrollbar]:w-1 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20 hover:[&::-webkit-scrollbar-thumb]:bg-muted-foreground/40 [&::-webkit-scrollbar-track]:bg-transparent">
      <BlockNoteView
        editor={editor}
        theme={isDark ? "dark" : "light"}
        editable={editable}
        onChange={() => {
          const document = editor.document as unknown as AnyBlock[];

          if (skipFirstChange.current) {
            skipFirstChange.current = false;
            seenBlockIdsRef.current = collectBlockIds(document);
            return;
          }

          if (seenBlockIdsRef.current) {
            const seen = seenBlockIdsRef.current;
            for (const block of collectMediaBlocks(document)) {
              if (!seen.has(block.id) && block.props?.textAlignment === "left") {
                editor.updateBlock(block.id, { props: { textAlignment: "center" } } as never);
              }
            }
          }
          seenBlockIdsRef.current = collectBlockIds(document);

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
