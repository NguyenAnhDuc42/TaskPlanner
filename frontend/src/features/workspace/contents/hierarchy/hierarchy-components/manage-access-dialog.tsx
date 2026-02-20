import { useEffect, useState } from "react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Loader2, ShieldCheck } from "lucide-react";
import { MemberSelector } from "./member-selector";
import { useParams } from "@tanstack/react-router";
import { toast } from "sonner";
import {
  useSpaceMembersAccess,
  useFolderMembersAccess,
  useListMembersAccess,
  useUpdateEntityAccessBulk,
} from "../hierarchy-api";
import { toAssignableAccessLevel } from "@/types/access-level";
import type { AssignableAccessLevel } from "@/types/access-level";

interface Props {
  entityId: string;
  entityName: string;
  layerType: "space" | "folder" | "list";
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function ManageAccessDialog({
  entityId,
  entityName,
  layerType,
  open,
  onOpenChange,
}: Props) {
  const { workspaceId } = useParams({ strict: false });
  const [selectedMemberIds, setSelectedMemberIds] = useState<string[]>([]);
  const [initialMemberIds, setInitialMemberIds] = useState<string[]>([]);
  const [creatorWorkspaceMemberId, setCreatorWorkspaceMemberId] = useState<
    string | undefined
  >(undefined);
  const [selectedAccessLevels, setSelectedAccessLevels] = useState<
    Record<string, AssignableAccessLevel>
  >({});

  const useAccessQuery =
    layerType === "space"
      ? useSpaceMembersAccess
      : layerType === "folder"
        ? useFolderMembersAccess
        : useListMembersAccess;

  const { data: accessMembers, isLoading: isLoadingAccess } =
    useAccessQuery(entityId);
  const updateAccessBulk = useUpdateEntityAccessBulk();

  useEffect(() => {
    if (!open || !accessMembers) return;

    const selectedIds = accessMembers.map((member) =>
      member.workspaceMemberId.toLowerCase(),
    );
    const levelMap: Record<string, AssignableAccessLevel> = {};
    const creator = accessMembers
      .find((member) => member.isCreator)
      ?.workspaceMemberId.toLowerCase();

    for (const member of accessMembers) {
      levelMap[member.workspaceMemberId.toLowerCase()] =
        toAssignableAccessLevel(member.accessLevel);
    }

    setSelectedMemberIds(selectedIds);
    setInitialMemberIds(selectedIds);
    setSelectedAccessLevels(levelMap);
    setCreatorWorkspaceMemberId(creator);
  }, [open, accessMembers]);

  const handleToggleMember = (memberId: string) => {
    setSelectedMemberIds((prev) => {
      const exists = prev.includes(memberId);
      if (exists) {
        return prev.filter((id) => id !== memberId);
      }
      return [...prev, memberId];
    });
  };

  const handleAccessLevelChange = (
    memberId: string,
    accessLevel: AssignableAccessLevel,
  ) => {
    setSelectedAccessLevels((prev) => ({
      ...prev,
      [memberId.toLowerCase()]: accessLevel,
    }));
  };

  const handleSave = async () => {
    try {
      const layerTypeValue =
        layerType === "space" ? 1 : layerType === "folder" ? 2 : 3;

      const members = [
        ...selectedMemberIds
          .filter((id) => id !== creatorWorkspaceMemberId)
          .map((id) => ({
            workspaceMemberId: id,
            accessLevel: selectedAccessLevels[id.toLowerCase()] || "Editor",
            isRemove: false,
          })),
        ...initialMemberIds
          .filter(
            (id) =>
              id !== creatorWorkspaceMemberId &&
              !selectedMemberIds.includes(id),
          )
          .map((id) => ({
            workspaceMemberId: id,
            isRemove: true,
          })),
      ];

      await updateAccessBulk.mutateAsync({
        entityId,
        layerType: layerTypeValue,
        members,
      });

      toast.success("Access updated successfully");
      onOpenChange(false);
    } catch (error: unknown) {
      const errorMessage =
        error instanceof Error ? error.message : "Failed to update access";
      toast.error(errorMessage);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[425px]">
        <DialogHeader>
          <div className="flex items-center gap-2">
            <ShieldCheck className="h-5 w-5 text-primary" />
            <DialogTitle>Manage Access</DialogTitle>
          </div>
          <DialogDescription>
            Manage who can access <strong>{entityName}</strong>
          </DialogDescription>
        </DialogHeader>

        <div className="py-2">
          {isLoadingAccess ? (
            <div className="h-[300px] flex items-center justify-center">
              <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
            </div>
          ) : (
            <MemberSelector
              workspaceId={workspaceId || ""}
              selectedIds={selectedMemberIds}
              selectedAccessLevels={selectedAccessLevels}
              creatorWorkspaceMemberId={creatorWorkspaceMemberId}
              onToggle={handleToggleMember}
              onAccessLevelChange={handleAccessLevelChange}
            />
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button
            onClick={handleSave}
            disabled={updateAccessBulk.isPending || isLoadingAccess}
          >
            {updateAccessBulk.isPending && (
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            )}
            Save Changes
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
