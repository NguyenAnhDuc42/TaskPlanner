import { BlockType } from "../block-type";

export interface DocumentBlockRecord {
    id: string;
    documentId: string;
    type: BlockType;
    content: string;
    orderKey: string;
}
