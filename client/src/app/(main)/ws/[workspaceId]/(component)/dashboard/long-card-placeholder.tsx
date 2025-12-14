
// client/src/app/(main)/ws/[workspaceId]/(component)/dashboard/long-card-placeholder.tsx
import React from 'react';

import { cn } from "@/lib/utils"; // Add this import

export const LongCardPlaceholder = ({ className }: { className?: string }) => {
  return (
    <div className={cn("flex-1 border border-border rounded-lg p-4 flex items-center justify-center", className)}>
      <p className="text-muted-foreground">Long Card Placeholder</p>
    </div>
  );
};
