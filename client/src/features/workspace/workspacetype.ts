import { WorkspaceSummary } from "@/types/workspace";
import { Role } from "@/utils/role-utils";

export interface CreateWorkspaceRequest {
  name: string;
  description: string;
  icon: string;
  color: string;
  isPrivate: boolean;
}
export interface CreateWorkspaceResponse {
  workspaceId: string;
  message: string;
}

export interface AddMembersBody {
   emails: string[];
   role : Role
   
}
export interface UpdateMembersBody{
    memberIds: string[];
    role: Role;
}
export interface AddMembersResponse{
  emails: string[];
  message: string;
}

export interface GroupWorkspace {
  currentWorkspace: WorkspaceSummary;
  otherWorkspaces: WorkspaceSummary[];
}


export interface Workspaces {workspaces: Workspace[];}
export interface Workspace { id: string; name: string; icon: string;}


export interface GetHierarchyRequest{id : string}


export interface Hierarchy {spaces : SpaceNode[]}
export interface SpaceNode {id : string ,name : string,icon : string,color : string, directLists : ListNode[] | null,folders : FolderNode[] | null }
export interface FolderNode {id : string ,name : string,lists : ListNode[]}
export interface ListNode{id : string ,name : string}