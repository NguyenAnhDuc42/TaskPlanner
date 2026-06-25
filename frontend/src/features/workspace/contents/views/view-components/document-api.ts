import { workspaceApi } from "@/store/workspaceApi";
import type { DocumentBlockRecord } from "@/types/document/document-block-record";
import { BlockType } from "@/types/block-type";

export interface DocumentBlockValue {
  id?: string;       // present for both updates AND new blocks (client-generated UUID)
  content: string;   // JSON of the node without id attr
  orderKey: string;
  blockType: BlockType;
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
      // Clear cache immediately on unmount so next mount always fetches fresh from DB
      keepUnusedDataFor: 0,
    }),

    updateDocumentBlocks: build.mutation<void, { documentId: string; blocks: DocumentBlockValue[] }>({
      query: ({ documentId, blocks }) => ({
        url: `/documents/${documentId}/blocks`,
        method: "PUT",
        data: blocks,
      }),
      // No invalidatesTags — snapshot owns local state, keepUnusedDataFor:0 handles fresh loads
    }),
  }),
});

export const {
  useGetDocumentBlocksQuery,
  useUpdateDocumentBlocksMutation,
} = documentApi;
