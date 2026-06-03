import { workspaceApi } from "@/store/workspaceApi";
import type { DocumentBlockRecord } from "@/types/document/document-block-record";
import { BlockType } from "@/types/block-type";

export interface DocumentBlockValue {
  id?: string;
  content: string;
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
    }),
    updateDocumentBlocks: build.mutation<void, { documentId: string; blocks: DocumentBlockValue[] }>({
      query: ({ documentId, blocks }) => ({
        url: `/documents/${documentId}/blocks`,
        method: "PUT",
        data: blocks,
      }),
      invalidatesTags: (_result, _error, { documentId }) => [{ type: "Documents" as const, id: `blocks-${documentId}` }],
    }),
  }),
});

export const {
  useGetDocumentBlocksQuery,
  useUpdateDocumentBlocksMutation,
} = documentApi;
