import { BlockType } from "../block-type";

export interface DocumentBlockRecord {
    id: string;
    type: BlockType;
    content: string;
    orderKey: string;
}
