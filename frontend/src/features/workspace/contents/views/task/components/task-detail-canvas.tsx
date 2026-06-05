import { useState, useEffect } from "react";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { UserPlus, Check, X, Send } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { useSelector } from "react-redux";
import { memberSelectors, assigneeSelectors, entityAccessSelectors, taskSelectors } from "@/store/entityStore";
import type { RootState } from "@/store";
import { useWorkspace } from "@/features/workspace/context/workspace-provider";
import { useGetEntityAccessQuery } from "../../space/space-api";
import { StatusSelect } from "@/components/status-select";
import { PrioritySelect } from "@/components/priority-select";
import { PriorityBadge } from "@/components/priority-badge";
import { SimpleDatePicker } from "@/features/workspace/components/forms/form-elements";
import { UniversalPicker } from "@/components/universal-picker";
import { DynamicIcon } from "@/components/dynamic-icon";
import { BlockEditor } from "@/components/blockbase/block-editor";
import { ViewSkeleton } from "@/components/view-skeleton";
import {
  useGetTaskDetailQuery,
  useUpdateTaskMutation,
  useGetTaskAssigneesQuery,
  useUpdateTaskAssigneesMutation,
  useGetTaskCommentsQuery,
  useAddCommentMutation,
} from "../task-api";

interface TaskDetailCanvasProps {
  taskId?: string;
}

export function TaskDetailCanvas({ taskId }: TaskDetailCanvasProps) {
  const { registry } = useWorkspace();
  const allMembers = useSelector(memberSelectors.selectAll);

  const { isLoading } = useGetTaskDetailQuery(taskId || "", {
    skip: !taskId,
  });
  const task = useSelector((state: RootState) => taskSelectors.selectById(state, taskId || ""));
  useGetTaskAssigneesQuery(taskId || "", {
    skip: !taskId,
  });
  const { data: comments = [] } = useGetTaskCommentsQuery(taskId || "", {
    skip: !taskId,
  });

  const [updateTask] = useUpdateTaskMutation();
  const [updateTaskAssignees] = useUpdateTaskAssigneesMutation();
  const [addComment] = useAddCommentMutation();

  const allAssignees = useSelector(assigneeSelectors.selectAll);
  const assignees = allAssignees.filter(a => a.taskId === taskId);

  // Retrieve the task's space to check privacy
  const space = useSelector((state: any) => state.spaces.entities[task?.spaceId || ""]);

  // Fetch access permissions if the space is private
  useGetEntityAccessQuery(task?.spaceId || "", {
    skip: !task?.spaceId || !space?.isPrivate,
  });

  const entityAccessList = useSelector(entityAccessSelectors.selectAll).filter(ea => ea.spaceId === task?.spaceId);
  const spaceAccessList = entityAccessList.filter(ea => ea.haveAccess);

  const [localName, setLocalName] = useState("");
  const [assigneeSearch, setAssigneeSearch] = useState("");
  const [newCommentText, setNewCommentText] = useState("");

  useEffect(() => {
    if (task?.name) {
      setLocalName(task.name);
    }
  }, [task?.name]);

  if (!taskId) {
    return (
      <div className="flex items-center justify-center h-full text-muted-foreground text-sm italic">
        No task selected.
      </div>
    );
  }

  if (isLoading || !task) {
    return <ViewSkeleton />;
  }

  const handleNameBlur = () => {
    if (localName.trim() && localName !== task.name) {
      updateTask({ taskId, patches: { name: localName.trim() } });
    }
  };

  const handleStatusChange = (statusId: string) => {
    updateTask({ taskId, patches: { statusId } });
  };

  const handlePriorityChange = (priority: any) => {
    updateTask({ taskId, patches: { priority } });
  };

  const handleStartDateChange = (date: Date | undefined) => {
    updateTask({ taskId, patches: { startDate: date ? date.toISOString() : undefined } });
  };

  const handleDueDateChange = (date: Date | undefined) => {
    updateTask({ taskId, patches: { dueDate: date ? date.toISOString() : undefined } });
  };

  const handleToggleAssignee = (memberId: string) => {
    const existing = assignees.find((a) => a.workspaceMemberId === memberId);
    const isAssigned = !!existing;
    updateTaskAssignees({
      taskId,
      changes: [{ id: existing?.id, memberId, isDelete: isAssigned }]
    });
  };

  const handleSendComment = async (e?: React.FormEvent) => {
    e?.preventDefault();
    if (!newCommentText.trim()) return;
    try {
      await addComment({ taskId, content: newCommentText.trim() }).unwrap();
      setNewCommentText("");
    } catch {}
  };

  const allowedMembers = space?.isPrivate
    ? allMembers.filter(m => spaceAccessList.some(ea => ea.workspaceMemberId === m.id || ea.workspaceMemberId === m.workspaceMemberId))
    : allMembers;

  const filteredMembers = allowedMembers.filter((m) =>
    m.name.toLowerCase().includes(assigneeSearch.toLowerCase()) ||
    m.email?.toLowerCase().includes(assigneeSearch.toLowerCase())
  );

  return (
    <div className="flex flex-col h-full w-full bg-transparent overflow-hidden">
      {/* Task Content Scroll Area */}
      <div className="flex-1 overflow-y-auto [&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20 hover:[&::-webkit-scrollbar-thumb]:bg-muted-foreground/45 [&::-webkit-scrollbar-track]:bg-transparent">
        <div className="w-full p-4 md:p-8 space-y-6">
          {/* Header Title Area */}
          <div className="flex items-start gap-3">
            <Popover>
              <PopoverTrigger asChild>
                <button className="h-9 w-9 flex items-center justify-center rounded-lg border border-border/50 bg-muted/20 hover:bg-muted/40 transition-colors shrink-0">
                  <DynamicIcon name={task.icon || "CheckSquare"} color={task.color || ""} size={20} />
                </button>
              </PopoverTrigger>
              <PopoverContent className="p-0 border-none bg-transparent shadow-none" align="start">
                <UniversalPicker
                  selectedIcon={task.icon || "CheckSquare"}
                  selectedColor={task.color || "#6366f1"}
                  onSelect={(icon, color) => {
                    updateTask({ taskId, patches: { icon, color } });
                  }}
                />
              </PopoverContent>
            </Popover>

            <Input
              value={localName}
              onChange={(e) => setLocalName(e.target.value)}
              onBlur={handleNameBlur}
              placeholder="Untitled Task"
              className="text-2xl font-black text-foreground border-none p-0 focus-visible:ring-0 bg-transparent h-auto"
            />
          </div>

          {/* Properties Grid (Notion-style, wraps nicely in Context Area) */}
          <div className="grid grid-cols-[100px_1fr] gap-y-3.5 gap-x-4 items-center text-sm pb-6 border-b border-border/30">
            {/* Status */}
            <span className="font-mono text-[10px] uppercase tracking-wider opacity-50 shrink-0">Status</span>
            <div>
              <StatusSelect
                value={task.statusId}
                onChange={handleStatusChange}
                workflowId={task.parentWorkflowId}
              />
            </div>

            {/* Priority */}
            <span className="font-mono text-[10px] uppercase tracking-wider opacity-50 shrink-0">Priority</span>
            <div>
              <PrioritySelect
                value={task.priority}
                onChange={handlePriorityChange}
                trigger={
                  <button className="h-7 px-2 flex items-center gap-1.5 text-xs border border-border/50 rounded-md bg-muted/10 hover:bg-muted/20 text-foreground transition-all">
                    <PriorityBadge priority={task.priority} className="border-none bg-transparent p-0" />
                  </button>
                }
              />
            </div>

            {/* Assignees */}
            <span className="font-mono text-[10px] uppercase tracking-wider opacity-50 shrink-0">Assignees</span>
            <div className="flex flex-wrap items-center gap-1.5 min-h-6">
              {assignees.map((assignee) => {
                const member = registry.memberMap[assignee.workspaceMemberId] || allMembers.find(m => m.id === assignee.workspaceMemberId || m.workspaceMemberId === assignee.workspaceMemberId);
                if (!member) return null;
                const initials = member.name.split(" ").map((n: string) => n[0]).join("").slice(0, 2).toUpperCase();
                return (
                  <div key={assignee.workspaceMemberId} className="flex items-center gap-1 bg-muted/30 border border-border/50 rounded-full pl-1 pr-2 py-0.5 text-xs">
                    <Avatar className="h-4 w-4">
                      {member.avatarUrl && <AvatarImage src={member.avatarUrl} alt={member.name} />}
                      <AvatarFallback className="text-[7px] bg-primary/20 text-primary">{initials}</AvatarFallback>
                    </Avatar>
                    <span className="max-w-[80px] truncate">{member.name}</span>
                    <button
                      type="button"
                      onClick={() => handleToggleAssignee(assignee.workspaceMemberId)}
                      className="text-muted-foreground hover:text-foreground ml-1"
                    >
                      <X className="h-3 w-3" />
                    </button>
                  </div>
                );
              })}

              <Popover>
                <PopoverTrigger asChild>
                  <Button variant="ghost" size="sm" className="h-6 px-2 text-xs text-muted-foreground hover:text-foreground border border-dashed border-border/50 hover:bg-muted/50 rounded-full">
                    <UserPlus className="h-3 w-3 mr-1" /> Add
                  </Button>
                </PopoverTrigger>
                <PopoverContent className="w-60 p-2 bg-popover border border-border shadow-md rounded-md" align="start">
                  <div className="px-2 py-1.5 border-b border-border/10 mb-2">
                    <span className="text-[8px] font-black uppercase tracking-wider text-muted-foreground/50">Assign Members</span>
                  </div>
                  <Input
                    placeholder="Filter members..."
                    value={assigneeSearch}
                    onChange={(e) => setAssigneeSearch(e.target.value)}
                    className="h-8 text-xs mb-2 bg-muted/20 border-none"
                  />
                  <div className="max-h-40 overflow-y-auto flex flex-col gap-1">
                    {filteredMembers.map((member) => {
                      const memberId = member.workspaceMemberId || member.id;
                      const isAssigned = assignees.some((a) => a.workspaceMemberId === memberId);
                      const initials = member.name.split(" ").map((n: string) => n[0]).join("").slice(0, 2).toUpperCase();
                      return (
                        <button
                          key={memberId}
                          type="button"
                          onClick={() => handleToggleAssignee(memberId)}
                          className="w-full flex items-center justify-between px-2 py-1.5 text-xs text-left rounded-sm hover:bg-muted transition-colors"
                        >
                          <div className="flex items-center gap-2">
                            <Avatar className="h-5 w-5">
                              {member.avatarUrl && <AvatarImage src={member.avatarUrl} alt={member.name} />}
                              <AvatarFallback className="text-[8px] bg-primary/20 text-primary">{initials}</AvatarFallback>
                            </Avatar>
                            <span className="truncate">{member.name}</span>
                          </div>
                          {isAssigned && <Check className="h-3 w-3 text-primary" />}
                        </button>
                      );
                    })}
                  </div>
                </PopoverContent>
              </Popover>
            </div>

            {/* Start Date */}
            <span className="font-mono text-[10px] uppercase tracking-wider opacity-50 shrink-0">Start Date</span>
            <div>
              <SimpleDatePicker
                value={task.startDate ? new Date(task.startDate) : undefined}
                onChange={handleStartDateChange}
                label="Start Date"
              />
            </div>

            {/* Due Date */}
            <span className="font-mono text-[10px] uppercase tracking-wider opacity-50 shrink-0">Due Date</span>
            <div>
              <SimpleDatePicker
                value={task.dueDate ? new Date(task.dueDate) : undefined}
                onChange={handleDueDateChange}
                label="Due Date"
              />
            </div>
          </div>

          {/* Document Section (Rich Text Editor) */}
          <div className="space-y-3">
            <h3 className="font-mono text-[10px] uppercase tracking-widest text-muted-foreground/70 border-b border-border/50 pb-2">
              Document
            </h3>
            {task.defaultDocumentId ? (
              <div className="min-h-[150px] border border-border/10 rounded-lg p-2 bg-muted/5">
                <BlockEditor documentId={task.defaultDocumentId} placeholder="Type '/' for commands..." />
              </div>
            ) : (
              <div className="text-xs text-muted-foreground italic">No document available for this task.</div>
            )}
          </div>

          {/* Comments Section */}
          <div className="space-y-4 pt-6 border-t border-border/30">
            <h3 className="font-mono text-[10px] uppercase tracking-widest text-muted-foreground/70">
              Comments
            </h3>

            <div className="space-y-4 max-h-[300px] overflow-y-auto pr-2 [&::-webkit-scrollbar]:w-1 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20 hover:[&::-webkit-scrollbar-thumb]:bg-muted-foreground/45 [&::-webkit-scrollbar-track]:bg-transparent">
              {comments.map((comment) => {
                const creator = registry.memberMap[comment.creatorId] || allMembers.find((m) => m.id === comment.creatorId || m.workspaceMemberId === comment.creatorId);
                const name = creator?.name || "Unknown User";
                const initials = name.split(" ").map((n: string) => n[0]).join("").slice(0, 2).toUpperCase();
                return (
                  <div key={comment.id} className="flex items-start gap-3 text-sm">
                    <Avatar className="h-6 w-6 mt-0.5 shrink-0">
                      {creator?.avatarUrl && <AvatarImage src={creator.avatarUrl} alt={name} />}
                      <AvatarFallback className="text-[8px] bg-primary/20 text-primary">{initials}</AvatarFallback>
                    </Avatar>
                    <div className="flex-1 space-y-1">
                      <div className="flex items-baseline gap-2">
                        <span className="font-semibold text-foreground text-xs">{name}</span>
                        <span className="text-[10px] text-muted-foreground">
                          {new Date(comment.createdAt).toLocaleString()}
                        </span>
                      </div>
                      <p className="text-muted-foreground text-xs leading-relaxed">{comment.content}</p>
                    </div>
                  </div>
                );
              })}
              {comments.length === 0 && (
                <p className="text-xs text-muted-foreground/50 italic py-2">No comments yet. Start the conversation!</p>
              )}
            </div>

            <form onSubmit={handleSendComment} className="flex gap-2 mt-2">
              <Input
                placeholder="Write a comment..."
                value={newCommentText}
                onChange={(e) => setNewCommentText(e.target.value)}
                className="text-xs h-9 bg-muted/20 border-none"
              />
              <Button type="submit" size="icon" className="h-9 w-9 shrink-0" disabled={!newCommentText.trim()}>
                <Send className="h-4 w-4" />
              </Button>
            </form>
          </div>
        </div>
      </div>
    </div>
  );
}
