import { useEffect, useState } from "react";
import { useFolderMembersAccess, useUpdateFolder } from "../hierarchy-api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Loader2 } from "lucide-react";
import { ColorPicker } from "@/components/color-picker";
import IconPicker from "@/components/icon-picker";
import { Checkbox } from "@/components/ui/checkbox";
import { toast } from "sonner";
import { MemberSelector } from "./member-selector";
import { useParams } from "@tanstack/react-router";
import { cn } from "@/lib/utils";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import type { AssignableAccessLevel } from "@/types/access-level";
import { toAssignableAccessLevel } from "@/types/access-level";

interface Props {
  id: string;
  initialName: string;
  initialColor: string;
  initialIcon: string;
  initialIsPrivate: boolean;
  open?: boolean;
}

export function UpdateFolderForm({
  id,
  initialName,
  initialColor,
  initialIcon,
  initialIsPrivate,
  open = true,
}: Props) {
  const { workspaceId } = useParams({ strict: false });
  const [name, setName] = useState(initialName);
  const [color, setColor] = useState(initialColor);
  const [icon, setIcon] = useState(initialIcon);
  const [isPrivate, setIsPrivate] = useState(initialIsPrivate);
  const [selectedMemberIds, setSelectedMemberIds] = useState<string[]>([]);
  const [initialMemberIds, setInitialMemberIds] = useState<string[]>([]);
  const [creatorWorkspaceMemberId, setCreatorWorkspaceMemberId] = useState<
    string | undefined
  >(undefined);
  const [selectedAccessLevels, setSelectedAccessLevels] = useState<
    Record<string, AssignableAccessLevel>
  >({});
  const { data: accessMembers, refetch: refetchAccessMembers } =
    useFolderMembersAccess(id);

  const updateFolder = useUpdateFolder();

  useEffect(() => {
    if (!open) return;

    setName(initialName);
    setColor(initialColor);
    setIcon(initialIcon);
    setIsPrivate(initialIsPrivate);

    if (initialIsPrivate) {
      void refetchAccessMembers();
    } else {
      setSelectedMemberIds([]);
      setInitialMemberIds([]);
      setSelectedAccessLevels({});
      setCreatorWorkspaceMemberId(undefined);
    }
  }, [
    open,
    initialName,
    initialColor,
    initialIcon,
    initialIsPrivate,
    refetchAccessMembers,
  ]);

  useEffect(() => {
    if (!isPrivate || !accessMembers) return;

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
  }, [isPrivate, accessMembers]);

  const handleToggleMember = (memberId: string) => {
    setSelectedMemberIds((prev) => {
      const exists = prev.includes(memberId);

      if (exists) {
        setSelectedAccessLevels((current) => {
          const next = { ...current };
          delete next[memberId];
          return next;
        });
        return prev.filter((id) => id !== memberId);
      }

      setSelectedAccessLevels((current) => ({
        ...current,
        [memberId]: current[memberId] ?? "Editor",
      }));
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

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim()) return;

    try {
      await updateFolder.mutateAsync({
        folderId: id,
        name: name,
        color: color,
        icon: icon,
        isPrivate: isPrivate,
        membersToAddOrUpdate: [
          ...selectedMemberIds
            .filter(
              (workspaceMemberId) =>
                workspaceMemberId !== creatorWorkspaceMemberId,
            )
            .map((workspaceMemberId) => ({
              workspaceMemberId,
              accessLevel: toAssignableAccessLevel(
                selectedAccessLevels[workspaceMemberId.toLowerCase()],
              ),
              isRemove: false,
            })),
          ...initialMemberIds
            .filter(
              (workspaceMemberId) =>
                workspaceMemberId !== creatorWorkspaceMemberId &&
                !selectedMemberIds.includes(workspaceMemberId),
            )
            .map((workspaceMemberId) => ({
              workspaceMemberId,
              isRemove: true,
            })),
        ],
      });
      await refetchAccessMembers();
      toast.success("Folder updated successfully");
    } catch (error: any) {
      console.error("Failed to update folder", error);
      toast.error(error.message || "Failed to update folder");
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4 pt-2">
      <div
        className={cn(
          "max-h-[60vh] overflow-y-auto px-1",
          isPrivate
            ? "grid gap-4 lg:grid-cols-[minmax(0,1fr)_340px]"
            : "space-y-4",
        )}
      >
        <div className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="folder-name">Name</Label>
            <Input
              id="folder-name"
              placeholder="Enter folder name..."
              value={name}
              onChange={(e) => setName(e.target.value)}
              disabled={updateFolder.isPending}
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label>Color</Label>
              <ColorPicker value={color} onChange={setColor} />
            </div>
            <div className="space-y-2">
              <Label>Icon</Label>
              <IconPicker value={icon} onChange={setIcon} />
            </div>
          </div>

          <div className="flex items-center space-x-2">
            <Checkbox
              id="isFolderPrivate"
              checked={isPrivate}
              onCheckedChange={(checked) => setIsPrivate(checked === true)}
            />
            <Label
              htmlFor="isFolderPrivate"
              className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70"
            >
              Private folder
            </Label>
          </div>
        </div>

        {isPrivate && (
          <Card className="h-fit lg:sticky lg:top-0">
            <CardHeader>
              <CardTitle>Shared Members</CardTitle>
              <CardDescription>
                Select members who can access this folder.
              </CardDescription>
            </CardHeader>
            <CardContent>
              <MemberSelector
                workspaceId={workspaceId || ""}
                selectedIds={selectedMemberIds}
                selectedAccessLevels={selectedAccessLevels}
                creatorWorkspaceMemberId={creatorWorkspaceMemberId}
                onToggle={handleToggleMember}
                onAccessLevelChange={handleAccessLevelChange}
              />
            </CardContent>
          </Card>
        )}
      </div>

      <div className="flex justify-end gap-2 pt-4 border-t">
        <Button type="submit" disabled={updateFolder.isPending || !name.trim()}>
          {updateFolder.isPending && (
            <Loader2 className="mr-2 h-4 w-4 animate-spin" />
          )}
          Save Changes
        </Button>
      </div>
    </form>
  );
}
