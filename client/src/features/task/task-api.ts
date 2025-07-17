import apiClient from "@/lib/api-client";
import {  CreateTaskRequest, CreateTaskResponse, DeleteTaskResponse, UpdateTaskBodyRequest, UpdateTaskResponse } from "./task-type";
import { Task } from "@/types/task";

export const CreateTask  = async (data : CreateTaskRequest) : Promise<CreateTaskResponse> =>{
    const response = await apiClient.post<CreateTaskResponse>("/task", data);
    return response.data;
}



export const UpdateTask = async ({id,data}: {id : string, data : UpdateTaskBodyRequest}) : Promise<UpdateTaskResponse> => {
    const rep = await apiClient.put<UpdateTaskResponse>(`/task/${id}`, data);
    return rep.data;
}

export const DeleteTask = async (id : string) : Promise<DeleteTaskResponse> => {
    const rep = await apiClient.delete<DeleteTaskResponse>(`/task/${id}`);
    return rep.data;
}

export const GetTask = async (id : string) : Promise<Task> => {
    const rep = await apiClient.get<Task>(`/task/${id}`);
    return rep.data;
}