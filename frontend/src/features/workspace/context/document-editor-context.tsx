import { createContext, useCallback, useContext, useMemo, useRef, useState, type ReactNode, type RefCallback } from "react";

interface DocumentEditorClaim {
  token: object;
  element: HTMLDivElement;
  documentId: string;
  editable: boolean;
}

interface DocumentEditorContextValue {
  /** The active claim — top of the stack, or null when nothing claims the editor. */
  claim: DocumentEditorClaim | null;
  pushClaim: (claim: DocumentEditorClaim) => void;
  removeClaim: (token: object) => void;
}

const DocumentEditorContext = createContext<DocumentEditorContextValue | null>(null);

export function DocumentEditorProvider({ children }: { children: ReactNode }) {
  // A stack, not a single slot: two slots CAN be mounted at once — e.g. a space's documents view
  // plus a task opened in the context panel. The newest claim wins the editor; when it releases
  // (panel closes), the editor falls back to the previous claimant instead of going blank.
  const [claims, setClaims] = useState<DocumentEditorClaim[]>([]);

  const pushClaim = useCallback((claim: DocumentEditorClaim) => {
    setClaims((prev) => [...prev.filter((c) => c.token !== claim.token), claim]);
  }, []);

  const removeClaim = useCallback((token: object) => {
    setClaims((prev) => prev.filter((c) => c.token !== token));
  }, []);

  const value = useMemo(
    () => ({
      claim: claims.length > 0 ? claims[claims.length - 1] : null,
      pushClaim,
      removeClaim,
    }),
    [claims, pushClaim, removeClaim],
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

/**
 * Claims the shared document editor for a plain <div> placeholder rendered at the exact spot the
 * editor should visually appear. The editor itself lives outside the per-route remount boundary
 * (see DocumentEditorHost) — this hook just tells it "render into this element, showing this
 * document" for as long as the caller stays mounted.
 */
export function useDocumentEditorSlot(documentId: string | undefined, editable: boolean): RefCallback<HTMLDivElement> {
  const { pushClaim, removeClaim } = useDocumentEditorClaim();
  const tokenRef = useRef<object>({});

  return useCallback(
    (el: HTMLDivElement | null) => {
      if (el && documentId) {
        pushClaim({ token: tokenRef.current, element: el, documentId, editable });
      } else {
        // Removal is by own token only, so during a key-based remount it doesn't matter whether
        // the old slot's cleanup or the new slot's mount fires first — the new claim survives.
        removeClaim(tokenRef.current);
      }
    },
    [documentId, editable, pushClaim, removeClaim],
  );
}
