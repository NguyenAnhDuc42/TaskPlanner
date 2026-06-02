import * as React from "react";
import { useState } from "react";
import { FileText, Plus } from "lucide-react";
import { useSpaceDetail, useGetSpaceDocumentsQuery, useCreateSpaceDocumentMutation } from "../space-api";
import { DescriptionSection } from "../../../layer-detail/components/overview/description-section";

interface SpaceDocumentsPanelProps {
  spaceId: string;
}

export function SpaceDocumentsPanel({ spaceId }: SpaceDocumentsPanelProps) {
  const space = useSpaceDetail(spaceId);
  const { data: docTabs = [] } = useGetSpaceDocumentsQuery(spaceId);
  const [createSpaceDocument] = useCreateSpaceDocumentMutation();

  const [activeDocId, setActiveDocId] = useState<string | null>(null);
  const [isCreatingTab, setIsCreatingTab] = useState(false);
  const [newTabName, setNewTabName] = useState("");
  const isSavingTabRef = React.useRef(false);

  // Sync active document ID with defaultDocumentId on space load
  React.useEffect(() => {
    if (space?.defaultDocumentId && !activeDocId) {
      setActiveDocId(space.defaultDocumentId || null);
    }
  }, [space?.defaultDocumentId, activeDocId]);

  const handleAddTabClick = () => {
    setIsCreatingTab(true);
    setNewTabName("");
    isSavingTabRef.current = false;
  };

  const handleConfirmAddTab = async () => {
    if (isSavingTabRef.current) return;
    isSavingTabRef.current = true;

    const name = newTabName.trim();
    if (!name) {
      setIsCreatingTab(false);
      return;
    }

    try {
      const newDoc = await createSpaceDocument({ spaceId, name }).unwrap();
      setActiveDocId(newDoc.id);
    } catch (e) {
      console.error("Failed to create space document:", e);
    } finally {
      setIsCreatingTab(false);
    }
  };

  const handleCancelAddTab = () => {
    setIsCreatingTab(false);
  };

  if (!space) return null;

  return (
    <div className="rounded-xl border border-border/25 overflow-hidden bg-card/10 shadow-sm">
      {/* Tab bar */}
      <div className="flex items-center gap-0 bg-white/[0.02] border-b border-border/15 px-2 pt-1 select-none overflow-x-auto">
        {/* Default Overview doc tab */}
        {space.defaultDocumentId && (
          <div
            onClick={() => setActiveDocId(space.defaultDocumentId || null)}
            className={`flex items-center gap-1.5 pb-1.5 px-3 text-[11px] font-semibold border-b-2 cursor-pointer transition-all ${
              activeDocId === space.defaultDocumentId
                ? "border-primary text-foreground"
                : "border-transparent text-muted-foreground hover:text-foreground"
            }`}
          >
            <FileText className="h-3 w-3" />
            <span>Overview</span>
          </div>
        )}

        {/* Custom document tabs */}
        {docTabs.filter((tab) => !tab.isDefault).map((tab) => (
          <div
            key={tab.id}
            onClick={() => setActiveDocId(tab.id)}
            className={`flex items-center gap-1.5 pb-1.5 px-3 text-[11px] font-semibold border-b-2 cursor-pointer transition-all group relative ${
              activeDocId === tab.id
                ? "border-primary text-foreground"
                : "border-transparent text-muted-foreground hover:text-foreground"
            }`}
          >
            <FileText className="h-3 w-3 text-sky-400" />
            <span>{tab.name}</span>
          </div>
        ))}

        {/* Inline Temporary Creating Tab */}
        {isCreatingTab && (
          <div className="flex items-center gap-1.5 pb-1.5 px-3 text-[11px] font-semibold border-b-2 border-primary text-foreground">
            <FileText className="h-3 w-3 text-sky-400" />
            <input
              type="text"
              aria-label="New document name"
              value={newTabName}
              onChange={(e) => setNewTabName(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "Enter") handleConfirmAddTab();
                if (e.key === "Escape") handleCancelAddTab();
              }}
              onBlur={handleConfirmAddTab}
              className="bg-transparent border-none outline-none text-[11px] font-semibold text-foreground w-20 py-0 px-0.5"
              placeholder="Doc name..."
            />
          </div>
        )}

        {/* Add document button */}
        <button
          onClick={handleAddTabClick}
          className="mb-1.5 ml-auto flex items-center gap-1 px-2 py-0.5 rounded-md text-[10px] text-muted-foreground/60 hover:text-muted-foreground hover:bg-muted/40 transition-all cursor-pointer"
          title="Add document tab"
        >
          <Plus className="h-3 w-3" />
          <span>Add Tab</span>
        </button>
      </div>

      {/* Document content */}
      <div className="px-4 py-3">
        {activeDocId ? (
          <DescriptionSection key={activeDocId} documentId={activeDocId} />
        ) : (
          <div className="flex flex-col items-center justify-center py-10 text-center gap-2">
            <FileText className="h-8 w-8 text-muted-foreground/20" />
            <p className="text-xs text-muted-foreground/50 font-medium">No document selected.</p>
          </div>
        )}
      </div>
    </div>
  );
}
