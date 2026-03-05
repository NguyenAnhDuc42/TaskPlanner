export const viewsKeys = {
  all: ["views"] as const,
  listRoot: () => [...viewsKeys.all, "list"] as const,
  list: (layerId: string, layerType: string) =>
    [...viewsKeys.listRoot(), layerId, layerType] as const,
  dataRoot: () => [...viewsKeys.all, "data"] as const,
  data: (viewId: string) => [...viewsKeys.dataRoot(), viewId] as const,
};
