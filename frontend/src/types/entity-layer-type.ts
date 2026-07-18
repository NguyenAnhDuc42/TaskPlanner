export const EntityLayerType = {
  ProjectWorkspace: "ProjectWorkspace",
  ProjectSpace: "ProjectSpace",
  ProjectFolder: "ProjectFolder",
  ProjectTask: "ProjectTask",
  ProjectDocument: "ProjectDocument",
} as const;

export type EntityLayerType = (typeof EntityLayerType)[keyof typeof EntityLayerType];
