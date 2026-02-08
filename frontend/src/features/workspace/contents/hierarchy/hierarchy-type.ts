export interface ListHierarchy {
  id: string;
  name: string;
  color: string;
  icon: string;
  isPrivate: boolean;
}

export interface FolderHierarchy {
  id: string;
  name: string;
  color: string;
  icon: string;
  isPrivate: boolean;
  lists: ListHierarchy[];
}

export interface SpaceHierarchy {
  id: string;
  name: string;
  color: string;
  icon: string;
  isPrivate: boolean;
  folders: FolderHierarchy[];
  lists: ListHierarchy[];
}

export interface WorkspaceHierarchy {
  id: string;
  name: string;
  spaces: SpaceHierarchy[];
}
