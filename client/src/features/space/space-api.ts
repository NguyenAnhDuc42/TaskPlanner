import apiClient from "@/lib/api-client";
import {  CreateSpaceBody } from "./space-type";



export const CreateSpace = async (workspaceId : string,body : CreateSpaceBody) : Promise<string> => {
        const rep = await apiClient.post<string>(`workspace/${workspaceId}/spaces`, body);
        return rep.data;
}
