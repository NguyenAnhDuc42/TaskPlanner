import { observer } from "mobx-react-lite";
import { WifiOff } from "lucide-react";
import { useStore } from "@/stores/root.store";


export const OfflineBanner = observer(function OfflineBanner() {
  const { isOnline } = useStore();
  if (isOnline) return null;

  return (
    <div className="h-6 w-full shrink-0 flex items-center justify-center gap-1.5 bg-amber-500/15 text-amber-600 dark:text-amber-400 text-[10px] font-semibold rounded-md border border-amber-500/20">
      <WifiOff className="h-3 w-3" />
      You&apos;re offline — changes will sync when your connection is restored.
    </div>
  );
});
