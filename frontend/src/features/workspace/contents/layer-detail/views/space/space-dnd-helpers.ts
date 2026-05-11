export function calculateOrderKeys(
  targetIndex: number,
  activeId: string,
  items: any[]
): { previousItemOrderKey: string | undefined; nextItemOrderKey: string | undefined } {
  // Strip the active item from the array in case it's still present
  // (same-column reorder: active item exists; cross-column: it doesn't yet)
  const stripped = items.filter((i) => i.id !== activeId);
 
  // Clamp to valid range
  const clampedIndex = Math.max(0, Math.min(targetIndex, stripped.length));
 
  const previousItemOrderKey: string | undefined = stripped[clampedIndex - 1]?.orderKey;
  const nextItemOrderKey: string | undefined = stripped[clampedIndex]?.orderKey;
 
  return { previousItemOrderKey, nextItemOrderKey };
}