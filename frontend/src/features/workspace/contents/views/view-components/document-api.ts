import { workspaceApi } from "@/store/workspaceApi";
import type { DocumentBlockRecord } from "@/types/document/document-block-record";
import { BlockType } from "@/types/block-type";

export interface BlockSaveItem {
  id: string;
  type: BlockType;
  content: string;
  orderKey: string;
  isDeleted: boolean;
}

export const documentApi = workspaceApi.injectEndpoints({
  endpoints: (build) => ({
    getDocumentBlocks: build.query<DocumentBlockRecord[], string>({
      query: (documentId) => ({
        url: `/documents/${documentId}/blocks`,
        method: "GET",
      }),
      providesTags: (_result, _error, documentId) => [{ type: "Documents" as const, id: `blocks-${documentId}` }],
      keepUnusedDataFor: 0,
    }),

    saveDocumentBlocks: build.mutation<{ syncEventId: number }, { documentId: string; blocks: BlockSaveItem[] }>({
      query: ({ documentId, blocks }) => ({
        url: `/documents/${documentId}/blocks`,
        method: "PUT",
        data: blocks,
      }),
    }),
  }),
});

export const {
  useGetDocumentBlocksQuery,
  useSaveDocumentBlocksMutation,
} = documentApi;
