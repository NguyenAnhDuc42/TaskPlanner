import { useState } from "react";
import { useAuth } from "@/features/auth/auth-context";
import { useChangePassword, useUpdateProfile } from "@/features/auth/api";
import { LogOut, User, KeyRound, Pencil } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { toast } from "sonner";

export function Head() {
  const { user, logout } = useAuth();
  const updateProfile = useUpdateProfile();
  const changePassword = useChangePassword();

  const [isProfileOpen, setIsProfileOpen] = useState(false);
  const [isPasswordOpen, setIsPasswordOpen] = useState(false);
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");

  if (!user) return null;

  const openProfile = () => {
    setName(user.name);
    setEmail(user.email);
    setIsProfileOpen(true);
  };

  const handleProfileSave = async () => {
    if (!name.trim() || !email.trim()) return;

    try {
      await updateProfile.mutateAsync({
        name: name.trim(),
        email: email.trim(),
      });
      toast.success("Profile updated");
      setIsProfileOpen(false);
    } catch {
      toast.error("Failed to update profile");
    }
  };

  const handleChangePassword = async () => {
    if (!currentPassword || !newPassword) return;

    try {
      await changePassword.mutateAsync({ currentPassword, newPassword });
      toast.success("Password changed");
      setCurrentPassword("");
      setNewPassword("");
      setIsPasswordOpen(false);
    } catch {
      toast.error("Failed to change password");
    }
  };

  return (
    <div className="flex items-center justify-between px-6 py-4 bg-background border-b border-border">
      <div className="flex items-center gap-3">
        <div className="w-10 h-10 rounded-full bg-primary/10 flex items-center justify-center border border-primary/20">
          <User className="h-5 w-5 text-primary" />
        </div>
        <div>
          <h1 className="text-sm font-semibold text-foreground leading-none">
            {user.name}
          </h1>
          <p className="text-xs text-muted-foreground mt-1">{user.email}</p>
        </div>
      </div>

      <div className="flex items-center gap-2">
        <Dialog open={isProfileOpen} onOpenChange={setIsProfileOpen}>
          <DialogTrigger asChild>
            <Button
              variant="ghost"
              size="sm"
              className="flex items-center gap-2 text-muted-foreground"
              onClick={openProfile}
            >
              <Pencil className="h-4 w-4" />
              Edit
            </Button>
          </DialogTrigger>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Profile</DialogTitle>
              <DialogDescription>Update your account info.</DialogDescription>
            </DialogHeader>
            <div className="space-y-3">
              <Input
                placeholder="Name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                disabled={updateProfile.isPending}
              />
              <Input
                placeholder="Email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                disabled={updateProfile.isPending}
              />
            </div>
            <DialogFooter>
              <Button
                onClick={() => void handleProfileSave()}
                disabled={updateProfile.isPending || !name.trim() || !email.trim()}
              >
                Save
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>

        <Dialog open={isPasswordOpen} onOpenChange={setIsPasswordOpen}>
          <DialogTrigger asChild>
            <Button
              variant="ghost"
              size="sm"
              className="flex items-center gap-2 text-muted-foreground"
            >
              <KeyRound className="h-4 w-4" />
              Password
            </Button>
          </DialogTrigger>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Change Password</DialogTitle>
              <DialogDescription>
                Use your current password to set a new one.
              </DialogDescription>
            </DialogHeader>
            <div className="space-y-3">
              <Input
                type="password"
                placeholder="Current password"
                value={currentPassword}
                onChange={(e) => setCurrentPassword(e.target.value)}
                disabled={changePassword.isPending}
              />
              <Input
                type="password"
                placeholder="New password"
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                disabled={changePassword.isPending}
              />
            </div>
            <DialogFooter>
              <Button
                onClick={() => void handleChangePassword()}
                disabled={changePassword.isPending || !currentPassword || !newPassword}
              >
                Update Password
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>

        <Button
          variant="ghost"
          size="sm"
          onClick={logout}
          className="flex items-center gap-2 text-muted-foreground hover:text-destructive hover:bg-destructive/10 transition-colors font-mono"
        >
          <LogOut className="h-4 w-4" />
          Logout
        </Button>
      </div>
    </div>
  );
}

