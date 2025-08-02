export interface FolderSummary {
    id: string;
    name: string;
}

export interface FolderItems {
    folders: FolderSummary[];
}