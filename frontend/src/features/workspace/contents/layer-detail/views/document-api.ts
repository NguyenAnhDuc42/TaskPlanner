import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/api-client";

export function useDocumentBlocks(documentId: string) {
  return useQuery({
    queryKey: ["document-blocks", documentId],
    queryFn: async () => {
      const { data } = await api.get(`/documents/${documentId}/blocks`);
      return data;
    },
    enabled: !!documentId,
  });
}

export function useUpdateDocumentBlocks() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ documentId, blocks }: { documentId: string, blocks: any[] }) => {
      const { data } = await api.put(`/documents/${documentId}/blocks`, { blocks });
      return data;
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ["document-blocks", variables.documentId] });
    },
  });
}
