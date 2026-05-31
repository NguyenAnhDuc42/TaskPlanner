import { lazy, Suspense } from "react";
import { Loader2 } from "lucide-react";

// Lazy load the heavy editor component
const BlockEditor = lazy(() =>
  import("@/components/blockbase/block-editor").then((m) => ({ default: m.BlockEditor }))
);

interface DescriptionSectionProps {
  documentId: string;
}

export function DescriptionSection({ documentId }: DescriptionSectionProps) {
  return (
    <div className="w-full">
      <Suspense
        fallback={
          <div className="h-[320px] w-full flex items-center justify-center text-muted-foreground/30 gap-2.5 text-sm animate-pulse">
            <Loader2 className="h-4 w-4 animate-spin" />
            <span className="font-medium">Loading document…</span>
          </div>
        }
      >
        <BlockEditor
          documentId={documentId}
          placeholder="Write something, or type '/' for commands…"
        />
      </Suspense>
    </div>
  );
}
