import { createContext, useCallback, useContext, useMemo, useRef, useState, type ReactNode, type RefCallback } from "react";

interface DocumentEditorClaim {
  token: object;
  element: HTMLDivElement;
  documentId: string;
  editable: boolean;
}

export interface DocumentOutlineEntry {
  id: string;
  text: string;
  level: number;
}

interface DocumentOutlineState {
  documentId: string;
  outline: DocumentOutlineEntry[];
}

interface DocumentEditorContextValue {
  claim: DocumentEditorClaim | null;
  pushClaim: (claim: DocumentEditorClaim) => void;
  removeClaim: (token: object) => void;
  outlineState: DocumentOutlineState | null;
  setOutlineState: (state: DocumentOutlineState | null) => void;
  scrollToBlock: (blockId: string) => void;
}

const DocumentEditorContext = createContext<DocumentEditorContextValue | null>(null);

export function DocumentEditorProvider({ children }: { children: ReactNode }) {
  const [claims, setClaims] = useState<DocumentEditorClaim[]>([]);
  const [outlineState, setOutlineState] = useState<DocumentOutlineState | null>(null);

  const pushClaim = useCallback((claim: DocumentEditorClaim) => {
    setClaims((prev) => [...prev.filter((c) => c.token !== claim.token), claim]);
  }, []);

  const removeClaim = useCallback((token: object) => {
    setClaims((prev) => prev.filter((c) => c.token !== token));
  }, []);

  const claim = claims.length > 0 ? claims[claims.length - 1] : null;

  const scrollToBlock = useCallback((blockId: string) => {
    const root: ParentNode = claim?.element ?? document;
    root.querySelector(`[data-id="${blockId}"]`)?.scrollIntoView({ behavior: "smooth", block: "start" });
  }, [claim]);

  const value = useMemo(
    () => ({
      claim,
      pushClaim,
      removeClaim,
      outlineState,
      setOutlineState,
      scrollToBlock,
    }),
    [claim, pushClaim, removeClaim, outlineState, scrollToBlock],
  );

  return (
    <DocumentEditorContext.Provider value={value}>
      {children}
    </DocumentEditorContext.Provider>
  );
}

export function useDocumentEditorClaim() {
  const ctx = useContext(DocumentEditorContext);
  if (!ctx) throw new Error("useDocumentEditorClaim must be used within DocumentEditorProvider");
  return ctx;
}

export function useDocumentEditorSlot(documentId: string | undefined, editable: boolean): RefCallback<HTMLDivElement> {
  const { pushClaim, removeClaim } = useDocumentEditorClaim();
  const tokenRef = useRef<object>({});

  return useCallback(
    (el: HTMLDivElement | null) => {
      if (el && documentId) {
        pushClaim({ token: tokenRef.current, element: el, documentId, editable });
      } else {
        removeClaim(tokenRef.current);
      }
    },
    [documentId, editable, pushClaim, removeClaim],
  );
}

export function useDocumentOutline(documentId: string | undefined): { outline: DocumentOutlineEntry[]; navigate: (blockId: string) => void } {
  const { outlineState, scrollToBlock } = useDocumentEditorClaim();
  const outline = documentId && outlineState?.documentId === documentId ? outlineState.outline : [];
  return { outline, navigate: scrollToBlock };
}
