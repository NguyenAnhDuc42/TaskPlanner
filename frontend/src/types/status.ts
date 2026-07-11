export interface Status {
  id: string;
  // Optional "ancestor" tag, not an exclusive scope — Status is workspace-visible everywhere.
  // Mirrors how Task/Folder carry their own ancestor ids as a filter dimension, not a partition.
  spaceId?: string;
  name: string;
  color: string;
  orderKey: string;
}
