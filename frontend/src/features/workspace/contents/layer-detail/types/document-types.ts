import { BlockType } from "@/types/block-type";

export interface DocumentBlockValue {
  id?: string;
  content: string;
  orderKey: string;
  blockType: BlockType;
  isDeleted: boolean;
}

export interface DocumentBlockDto {
  id: string;
  content: string;
  orderKey: string;
  type: BlockType;
}

// Tiptap JSON node structure
export interface TiptapNode {
  type: string;
  attrs?: {
    id?: string;
    level?: number;
    [key: string]: any;
  };
  content?: Array<{
    type: string;
    text?: string;
    [key: string]: any;
  }>;
}

export interface TiptapDoc {
  type: "doc";
  content: TiptapNode[];
}
