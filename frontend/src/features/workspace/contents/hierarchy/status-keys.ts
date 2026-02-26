export const statusKeys = {
  all: ["statuses"] as const,
  list: (layerId: string, layerType: string) =>
    [...statusKeys.all, layerId, layerType] as const,
};
