import "@blocknote/mantine/style.css";
import "./block-editor.css";
import { useCallback, useEffect, useLayoutEffect, useMemo, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { cn } from "@/lib/utils";
import { BlockNoteSchema, defaultBlockSpecs, filterSuggestionItems, type PartialBlock, type Block } from "@blocknote/core";
import { createCodeBlockSpec } from "@blocknote/core/blocks";
import { codeBlockOptions } from "@blocknote/code-block";
import { useCreateBlockNote, SuggestionMenuController, getDefaultReactSlashMenuItems } from "@blocknote/react";
import { BlockNoteView } from "@blocknote/mantine";
import { useDocumentEditorClaim, type DocumentOutlineEntry } from "@/features/workspace/context/document-editor-context";
import { useBlockEditorSync } from "@/features/workspace/contents/views/view-components/use-block-editor-sync";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { api } from "@/lib/api-client";

const BLOCKED_SLASH_ITEMS = new Set(["Audio"]);
const MEDIA_BLOCK_TYPES = new Set(["image", "video", "file"]);

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

function collectText(node: unknown): string {
  if (Array.isArray(node)) return node.map(collectText).join("");
  if (!node || typeof node !== "object") return "";
  const obj = node as Record<string, unknown>;
  if (typeof obj.text === "string") return obj.text;
  return obj.content ? collectText(obj.content) : "";
}

function extractOutline(blocks: AnyBlock[]): DocumentOutlineEntry[] {
  const out: DocumentOutlineEntry[] = [];
  const visit = (list: AnyBlock[]) => {
    for (const block of list) {
      if (block.type === "heading") {
        out.push({
          id: block.id,
          text: collectText((block as Record<string, unknown>).content),
          level: ((block.props?.level as number) ?? 1),
        });
      }
      if (block.children?.length) visit(block.children);
    }
  };
  visit(blocks);
  return out;
}

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
  const { claim, setOutlineState } = useDocumentEditorClaim();
  const { workspaceId } = useWorkspaceRootStore();
  const [homeElement, setHomeElement] = useState<HTMLDivElement | null>(null);

  const editorContainer = useMemo(() => {
    const el = document.createElement("div");
    el.style.height = "100%";
    return el;
  }, []);

  const claimElement = claim?.element ?? null;
  useEffect(() => {
    const target = claimElement ?? homeElement;
    if (!target) return;
    target.appendChild(editorContainer);
    return () => {
      if (editorContainer.parentNode === target) target.removeChild(editorContainer);
    };
  }, [claimElement, homeElement, editorContainer]);

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

  const outlineTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const recomputeOutline = useCallback(() => {
    if (!documentId) return;
    setOutlineState({ documentId, outline: extractOutline(editor.document as unknown as AnyBlock[]) });
  }, [editor, documentId, setOutlineState]);
  const scheduleOutline = useCallback(() => {
    if (outlineTimerRef.current) clearTimeout(outlineTimerRef.current);
    outlineTimerRef.current = setTimeout(() => {
      outlineTimerRef.current = null;
      recomputeOutline();
    }, 800);
  }, [recomputeOutline]);
  useEffect(() => () => { if (outlineTimerRef.current) clearTimeout(outlineTimerRef.current); }, []);
  useEffect(() => () => setOutlineState(null), [documentId, setOutlineState]);

  const seenBlockIdsRef = useRef<Set<string> | null>(null);
  // BlockNote's onChange fires synchronously inside editor.transact (wired straight to Tiptap's
  // "update" event, which ProseMirror dispatches synchronously) — so it always fires *during*
  // the replaceBlocks call below, never after. A digest recorded after transact() returns is
  // therefore always one step too late to recognize the very echo it's meant to suppress: the
  // hash comparison in onChange would compare the freshly-applied content against whatever
  // digest was left over from the previous apply (or null), never match, and — because it was
  // only ever cleared on a match — stay armed forever, paying a full JSON.stringify(doc) on
  // every subsequent keystroke for the rest of the session for a comparison that can never
  // succeed again. A synchronous boolean flag sidesteps the ordering issue entirely: it only
  // needs to be true for the exact duration of the transact() call, which is exactly when the
  // echo's onChange fires.
  const isApplyingRef = useRef(false);
  const appliedRef = useRef<{ documentId: string; version: number } | null>(null);

  useLayoutEffect(() => {
    if (!documentId || !isReady) return;
    const prev = appliedRef.current;
    const isNewDocument = !prev || prev.documentId !== documentId;
    const isNewVersion = !isNewDocument && prev.version !== version;
    if (!isNewDocument && !isNewVersion) return;
    appliedRef.current = { documentId, version };
    seenBlockIdsRef.current = null;
    const contentToApply = (initialContent as PartialBlock[] | undefined) ?? EMPTY_DOCUMENT;
    isApplyingRef.current = true;
    editor.transact((tr) => {
      tr.setMeta("addToHistory", false);
      editor.replaceBlocks(editor.document, contentToApply);
    });
    isApplyingRef.current = false;
    clearUndoHistory(editor);
    recomputeOutline();
  }, [documentId, isReady, version, initialContent, editor, recomputeOutline]);

  const [showLoadingSkeleton, setShowLoadingSkeleton] = useState(false);
  const isLoadingDocument = Boolean(documentId) && !isReady;
  if (!isLoadingDocument && showLoadingSkeleton) {
    setShowLoadingSkeleton(false);
  }
  useEffect(() => {
    if (!isLoadingDocument) return;
    const timer = setTimeout(() => setShowLoadingSkeleton(true), 150);
    return () => clearTimeout(timer);
  }, [isLoadingDocument, documentId]);

  const isDark = document.documentElement.classList.contains("dark");

  const editorNode = (
    <div className="relative h-full">
      {showLoadingSkeleton && (
        <div className="absolute inset-0 z-10 bg-card flex flex-col gap-3 pt-2">
          <div className="h-4 w-2/5 rounded bg-muted/60 animate-pulse" />
          <div className="h-3 w-4/5 rounded bg-muted/40 animate-pulse" />
          <div className="h-3 w-3/5 rounded bg-muted/40 animate-pulse" />
          <div className="h-3 w-2/3 rounded bg-muted/40 animate-pulse" />
        </div>
      )}
      <div
        className={cn("h-full", isLoadingDocument && "invisible")}
      >
      <BlockNoteView
        editor={editor}
        theme={isDark ? "dark" : "light"}
        editable={editable}
        onChange={() => {
          const doc = editor.document as unknown as AnyBlock[];
          if (isApplyingRef.current) {
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
          scheduleOutline();

          if (editable && isReady) handleUpdate(editor.document as unknown as Block[]);
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
    </div>
  );

  return (
    <>
      <div
        ref={setHomeElement}
        style={{ position: "fixed", top: 0, left: 0, width: 0, height: 0, overflow: "hidden", visibility: "hidden", pointerEvents: "none" }}
      />
      {createPortal(editorNode, editorContainer)}
    </>
  );
}
