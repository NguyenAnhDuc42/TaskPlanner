export const RowAction = { Create: "Create", Update: "Update", Delete: "Delete" } as const;
export type RowAction = (typeof RowAction)[keyof typeof RowAction];
