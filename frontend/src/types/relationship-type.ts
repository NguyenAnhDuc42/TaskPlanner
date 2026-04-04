export const EntityLayerType = {
  ProjectWorkspace: "ProjectWorkspace",
  ProjectSpace: "ProjectSpace",
  ProjectFolder: "ProjectFolder",
  ProjectTask: "ProjectTask",
  ChatRoom: "ChatRoom",
} as const;

export type EntityLayerType = (typeof EntityLayerType)[keyof typeof EntityLayerType];
