import "@blocknote/mantine/style.css";
import "./block-editor.css";
import { useEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { BlockNoteSchema, defaultBlockSpecs, filterSuggestionItems, type PartialBlock, type Block } from "@blocknote/core";
import { createCodeBlockSpec } from "@blocknote/core/blocks";
import { codeBlockOptions } from "@blocknote/code-block";
import { useCreateBlockNote, SuggestionMenuController, getDefaultReactSlashMenuItems } from "@blocknote/react";
import { BlockNoteView } from "@blocknote/mantine";
import { useDocumentEditorClaim } from "@/features/workspace/context/document-editor-context";
import { useBlockEditorSync } from "@/features/workspace/contents/views/view-components/use-block-editor-sync";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { api } from "@/lib/api-client";

const BLOCKED_SLASH_ITEMS = new Set(["Audio"]);
const MEDIA_BLOCK_TYPES = new Set(["image", "video", "file"]);

// The default supportedLanguages list has ~48 entries — every one becomes a real <option> DOM
// node in the code block's language picker. Trimmed to languages this codebase actually uses.
const SUPPORTED_LANGUAGE_IDS = [
  "text", "javascript", "typescript", "jsx", "tsx", "python", "csharp",
  "sql", "json", "html", "css", "shellscript", "markdown", "yaml",
] as const;
const trimmedCodeBlockOptions = {
  ...codeBlockOptions,
  supportedLanguages: Object.fromEntries(
    SUPPORTED_LANGUAGE_IDS.map((id) => [id, codeBlockOptions.supportedLanguages[id]]),
  ) as typeof codeBlockOptions.supportedLanguages,
};

const schema = BlockNoteSchema.create({
  blockSpecs: {
    ...defaultBlockSpecs,
    codeBlock: createCodeBlockSpec(trimmedCodeBlockOptions),
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

const EMPTY_DOCUMENT: PartialBlock[] = [{ type: "paragraph" } as PartialBlock];

/**
 * Resets prosemirror-history's plugin state without touching the document, selection, or any
 * other plugin: reconfigure once without the history plugin (dropping its state), then
 * reconfigure back (fresh init). Because the editor instance now survives navigation, the undo
 * stack would otherwise carry the previous document's steps across a swap — and undoing them
 * against the new document produces garbage that autosave would then persist.
 *
 * Identified by prosemirror-history's plugin key string ("history$"); if it isn't found the
 * function is a no-op, which just means history isn't cleared — never anything worse.
 */
function clearUndoHistory(editor: { prosemirrorView?: { state: unknown; updateState(state: unknown): void } }) {
  const view = editor.prosemirrorView;
  if (!view) return;
  const state = view.state as {
    plugins: unknown[];
    reconfigure(config: { plugins: unknown[] }): { reconfigure(config: { plugins: unknown[] }): unknown };
  };
  const historyPlugin = state.plugins.find((p) => (p as { key?: string }).key === "history$");
  if (!historyPlugin) return;
  view.updateState(
    state
      .reconfigure({ plugins: state.plugins.filter((p) => p !== historyPlugin) })
      .reconfigure({ plugins: state.plugins }),
  );
}

export function DocumentEditorHost() {
  const { claim } = useDocumentEditorClaim();
  const [hasBeenClaimed, setHasBeenClaimed] = useState(false);
  if (claim && !hasBeenClaimed) {
    setHasBeenClaimed(true);
  }

  if (!hasBeenClaimed) return null;
  return <DocumentEditorHostInner />;
}

function DocumentEditorHostInner() {
  const { claim } = useDocumentEditorClaim();
  const { workspaceId } = useWorkspaceRootStore();
  const [homeElement, setHomeElement] = useState<HTMLDivElement | null>(null);

  const editor = useCreateBlockNote({
    schema,
    uploadFile: async (file: File) => {
      const formData = new FormData();
      formData.append("file", file);
      const { data } = await api.post<{ url: string }>("/attachments/sync/upload", formData, {
        headers: { "X-Workspace-Id": workspaceId },
      });
      return data.url;
    },
  });

  const documentId = claim?.documentId;
  const editable = claim?.editable ?? false;
  const { initialContent, handleUpdate, isReady, version } = useBlockEditorSync(documentId ?? "");

  const seenBlockIdsRef = useRef<Set<string> | null>(null);
  // Detects "was this onChange just the echo of our own replaceBlocks() call" by CONTENT, not
  // timing — see file header comment #3 for why timing-based detection is unsafe here.
  const lastAppliedContentRef = useRef<string | null>(null);
  // useBlockEditorSync's `version` resets to 0 every time documentId changes — including
  // revisiting a document you've already seen. A document-id change must ALWAYS force a reapply
  // regardless of version.
  const appliedRef = useRef<{ documentId: string; version: number } | null>(null);

  useEffect(() => {
    if (!documentId || !isReady) return;
    const prev = appliedRef.current;
    const isNewDocument = !prev || prev.documentId !== documentId;
    const isNewVersion = !isNewDocument && prev.version !== version;
    if (!isNewDocument && !isNewVersion) return;
    appliedRef.current = { documentId, version };
    seenBlockIdsRef.current = null;
    const contentToApply = (initialContent as PartialBlock[] | undefined) ?? EMPTY_DOCUMENT;
    // The whole swap goes into ONE transaction stamped addToHistory: false — nested transact
    // calls (editor.document, replaceBlocks) reuse the active transaction, and prosemirror-history
    // skips recording it. Without this, the content-load itself is the top undo entry: Ctrl+Z on a
    // freshly opened document undoes the load (empty editor), and autosave then deletes every block.
    editor.transact((tr) => {
      tr.setMeta("addToHistory", false);
      editor.replaceBlocks(editor.document, contentToApply);
    });
    // Drop whatever undo history the previous document left behind — same net effect the old
    // remount-per-navigation architecture had, where every swap started with a fresh editor.
    clearUndoHistory(editor);
    lastAppliedContentRef.current = JSON.stringify(editor.document);
  }, [documentId, isReady, version, initialContent, editor]);

  const isDark = document.documentElement.classList.contains("dark");

  const editorNode = (
    <div className="h-full">
      <BlockNoteView
        editor={editor}
        theme={isDark ? "dark" : "light"}
        editable={editable}
        onChange={() => {
          const doc = editor.document as unknown as AnyBlock[];
          const docJson = JSON.stringify(doc);
          const isOwnEcho = lastAppliedContentRef.current !== null && docJson === lastAppliedContentRef.current;

          if (isOwnEcho) {
            lastAppliedContentRef.current = null;
            seenBlockIdsRef.current = collectBlockIds(doc);
            return;
          }

          if (seenBlockIdsRef.current) {
            const seen = seenBlockIdsRef.current;
            for (const block of collectMediaBlocks(doc)) {
              if (!seen.has(block.id) && block.props?.textAlignment === "left") {
                editor.updateBlock(block.id, { props: { textAlignment: "center" } } as never);
              }
            }
          }
          seenBlockIdsRef.current = collectBlockIds(doc);

          if (editable) handleUpdate(editor.document as unknown as Block[]);
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

  return (
    <>
      <div
        ref={setHomeElement}
        style={{ position: "fixed", top: 0, left: 0, width: 0, height: 0, overflow: "hidden", visibility: "hidden", pointerEvents: "none" }}
      />
      {homeElement && createPortal(editorNode, claim?.element ?? homeElement)}
    </>
  );
}
