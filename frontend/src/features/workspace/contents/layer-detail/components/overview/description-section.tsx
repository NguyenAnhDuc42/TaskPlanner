import { lazy, Suspense } from "react";
import { Loader2 } from "lucide-react";

// Lazy load the heavy editor component
const BlockEditor = lazy(() => import("@/components/blockbase/block-editor").then(m => ({ default: m.BlockEditor })));

interface DescriptionSectionProps {
  documentId: string;
}

export function DescriptionSection({ documentId }: DescriptionSectionProps) {
  return (
    <div className="relative group">
      <div className="min-h-[400px]">
        <Suspense fallback={
          <div className="h-[400px] w-full rounded-xl border border-border/50 bg-muted/5 flex items-center justify-center text-muted-foreground/20 italic text-sm gap-3 animate-pulse">
            <Loader2 className="h-4 w-4 animate-spin" />
            <span>Initializing document engine...</span>
          </div>
        }>
          <BlockEditor
            documentId={documentId}
            placeholder="Define the strategic objectives and operational scope for this layer..."
          />
        </Suspense>
      </div>
    </div>
  );
}
