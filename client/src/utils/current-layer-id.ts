import { useParams } from "next/navigation";

export function useWorkspaceId() {
    const params = useParams<{workspaceId : string}>();
    return params.workspaceId;
}
export function useSpaceId() {
    const params = useParams<{spaceId : string}>();
    return params.spaceId;
}
export function useFolderId() {
    const params = useParams<{folderId : string}>();
    return params.folderId;
}
export function useTaskId() {
    const params = useParams<{taskId : string}>();
    return params.taskId;
}
export function useListId() {
    const params = useParams<{listId : string}>();
    return params.listId;
}