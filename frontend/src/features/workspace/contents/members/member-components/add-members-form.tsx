"use client";

import * as React from "react";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Field, FieldLabel } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Checkbox } from "@/components/ui/checkbox";
import { X, Trash2, UserPlus, Mail } from "lucide-react";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";
import { cn } from "@/lib/utils";

type InviteItem = {
  id: string;
  email: string;
  role: "Admin" | "Member" | "Guest";
  selected: boolean;
};

type Props = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSubmit?: (data: any) => Promise<any> | void;
  isLoading?: boolean;
};

export function AddMembersForm({
  open,
  onOpenChange,
  onSubmit,
  isLoading,
}: Props) {
  const [inputValue, setInputValue] = React.useState("");
  const [invites, setInvites] = React.useState<InviteItem[]>([]);
  const [enableEmail, setEnableEmail] = React.useState(true);
  const [message, setMessage] = React.useState("");
  const [batchRole, setBatchRole] = React.useState<
    "Admin" | "Member" | "Guest"
  >("Guest");

  const selectedCount = invites.filter((i) => i.selected).length;
  const allSelected = invites.length > 0 && selectedCount === invites.length;

  const addEmails = (emailsStr: string) => {
    const rawEmails = emailsStr.split(/[,\s;]+/).filter((e) => e.trim() !== "");
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

    const newInvites: InviteItem[] = rawEmails
      .filter((email) => emailRegex.test(email))
      .filter((email) => !invites.some((existing) => existing.email === email))
      .map((email) => ({
        id: Math.random().toString(36).substring(7),
        email,
        role: batchRole,
        selected: false,
      }));

    if (newInvites.length > 0) {
      setInvites((prev) => [...prev, ...newInvites]);
      setInputValue("");
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "Enter" || e.key === "," || e.key === " ") {
      e.preventDefault();
      addEmails(inputValue);
    }
  };

  const handlePaste = (e: React.ClipboardEvent) => {
    e.preventDefault();
    const pastedData = e.clipboardData.getData("text");
    addEmails(pastedData);
  };

  const toggleSelect = (id: string) => {
    setInvites((prev) =>
      prev.map((inv) =>
        inv.id === id ? { ...inv, selected: !inv.selected } : inv,
      ),
    );
  };

  const toggleSelectAll = () => {
    setInvites((prev) =>
      prev.map((inv) => ({ ...inv, selected: !allSelected })),
    );
  };

  const handleRoleChange = (role: "Admin" | "Member" | "Guest") => {
    setBatchRole(role);
    if (selectedCount > 0) {
      setInvites((prev) =>
        prev.map((inv) => (inv.selected ? { ...inv, role } : inv)),
      );
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (invites.length === 0 || isLoading) return;

    const targets =
      selectedCount > 0 ? invites.filter((i) => i.selected) : invites;

    const payload = {
      members: targets.map((inv) => ({ email: inv.email, role: inv.role })),
      enableEmail,
      message,
    };

    try {
      await onSubmit?.(payload);
      const targetIds = targets.map((t) => t.id);
      const remaining = invites.filter((inv) => !targetIds.includes(inv.id));
      setInvites(remaining);

      if (remaining.length === 0) {
        onOpenChange(false);
      }
    } catch (err) {
      // toast handled by useMutation
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-xl p-0 overflow-hidden flex flex-col max-h-[90vh] rounded-sm">
        <DialogHeader className="p-6 pb-2">
          <div className="flex items-center gap-2 mb-1">
            <div className="p-2 bg-primary/10 rounded-sm text-primary">
              <UserPlus className="h-5 w-5" />
            </div>
            <DialogTitle className="text-xl">Invite Members</DialogTitle>
          </div>
          <DialogDescription className="text-xs text-muted-foreground">
            Add people to your workspace. Invited members will disappear from
            the list.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="flex-1 flex flex-col min-h-0">
          <ScrollArea className="flex-1 px-6">
            <div className="space-y-6 py-4">
              <Field>
                <FieldLabel className="mb-2 text-xs font-mono uppercase tracking-widest text-muted-foreground">
                  Emails
                </FieldLabel>
                <div className="relative group">
                  <div className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground group-focus-within:text-primary transition-colors">
                    <Mail className="h-4 w-4" />
                  </div>
                  <Input
                    placeholder="john@example.com, sara@team.org..."
                    value={inputValue}
                    onChange={(e) => setInputValue(e.target.value)}
                    onKeyDown={handleKeyDown}
                    onPaste={handlePaste}
                    className="pl-10 h-10 bg-card border-border border-dashed focus:border-solid transition-all rounded-sm text-sm"
                    disabled={isLoading}
                  />
                </div>
              </Field>

              {invites.length > 0 && (
                <div className="space-y-4">
                  <div className="flex items-center justify-between px-1">
                    <div className="flex items-center gap-2">
                      <Checkbox
                        id="select-all"
                        checked={allSelected}
                        onCheckedChange={toggleSelectAll}
                        className="rounded-sm"
                      />
                      <label
                        htmlFor="select-all"
                        className="text-[10px] font-mono text-muted-foreground cursor-pointer select-none"
                      >
                        {selectedCount > 0
                          ? `SELECTED ${selectedCount}`
                          : "SELECT ALL"}
                      </label>
                    </div>

                    {selectedCount > 0 && (
                      <Button
                        variant="ghost"
                        size="xs"
                        className="h-7 px-2 text-muted-foreground hover:text-destructive gap-1.5 text-[10px] uppercase font-mono"
                        onClick={() =>
                          setInvites((prev) => prev.filter((i) => !i.selected))
                        }
                      >
                        <Trash2 className="h-3 w-3" />
                        Remove
                      </Button>
                    )}
                  </div>

                  <div className="flex flex-wrap gap-1 p-2 rounded-sm border border-border bg-card/10 min-h-[60px] align-content-start">
                    {invites.map((inv) => (
                      <button
                        key={inv.id}
                        type="button"
                        onClick={() => toggleSelect(inv.id)}
                        className={cn(
                          "group relative flex items-center gap-1 px-1.5 py-0.5 rounded-sm border transition-all text-[10px] font-medium",
                          inv.selected
                            ? "bg-primary border-primary text-primary-foreground"
                            : "bg-background border-border hover:border-primary/50 text-foreground",
                        )}
                      >
                        <span className="max-w-[100px] truncate">
                          {inv.email}
                        </span>
                        <div
                          className={cn(
                            "flex items-center pl-1 ml-1 border-l text-[8px] opacity-70",
                            inv.selected
                              ? "border-primary-foreground/30"
                              : "border-border text-muted-foreground",
                          )}
                        >
                          {inv.role[0]}
                        </div>
                        <X
                          className="h-2 w-2 ml-0.5 opacity-60 hover:opacity-100"
                          onClick={(e) => {
                            e.stopPropagation();
                            setInvites((prev) =>
                              prev.filter((i) => i.id !== inv.id),
                            );
                          }}
                        />
                      </button>
                    ))}
                  </div>

                  <div className="pt-2">
                    <RadioGroup
                      value={batchRole}
                      onValueChange={(v) => handleRoleChange(v as any)}
                      className="grid grid-cols-3 gap-2"
                    >
                      {["Guest", "Member", "Admin"].map((role) => (
                        <FieldLabel htmlFor={role} key={role}>
                          <Field orientation="horizontal">
                            <RadioGroupItem value={role} id={role} />
                            <div className="font-medium text-xs">{role}</div>
                          </Field>
                        </FieldLabel>
                      ))}
                    </RadioGroup>
                  </div>
                </div>
              )}

              <div className="pt-4 border-t border-border space-y-4">
                <div className="flex items-center gap-3 p-3 rounded-sm bg-primary/5 border border-primary/10">
                  <Checkbox
                    id="enable-email"
                    checked={enableEmail}
                    onCheckedChange={(checked) => setEnableEmail(!!checked)}
                    className="rounded-sm"
                  />
                  <div className="grid gap-1 leading-none">
                    <label
                      htmlFor="enable-email"
                      className="text-xs font-mono uppercase tracking-tighter cursor-pointer"
                    >
                      Send email invitations
                    </label>
                  </div>
                </div>

                <Field>
                  <FieldLabel className="mb-2 text-[10px] font-mono uppercase tracking-widest text-muted-foreground">
                    Personal Message
                  </FieldLabel>
                  <Textarea
                    placeholder="Hey! Join our new workspace..."
                    value={message}
                    onChange={(e) => setMessage(e.target.value)}
                    rows={2}
                    className="resize-none bg-card rounded-sm text-xs font-mono"
                    disabled={isLoading}
                  />
                </Field>
              </div>
            </div>
          </ScrollArea>

          <div className="p-6 border-t border-border bg-card">
            <Button
              type="submit"
              disabled={invites.length === 0 || isLoading}
              className="w-full h-10 bg-primary hover:bg-primary/90 text-primary-foreground font-bold font-mono tracking-wider rounded-sm"
            >
              {isLoading
                ? "SENDING..."
                : selectedCount > 0
                  ? `INVITE ${selectedCount} SELECTED AS ${batchRole.toUpperCase()}`
                  : `INVITE ${invites.length} MEMBERS`}
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}
