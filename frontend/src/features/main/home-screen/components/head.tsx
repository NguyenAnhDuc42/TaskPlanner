import { useState } from "react";
import { useAuth } from "@/features/auth/auth-context";
import { useChangePassword, useUpdateProfile } from "@/features/auth/api";
import { LogOut, KeyRound, Pencil, Bell, Settings, HelpCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { toast } from "sonner";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"

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
      await updateProfile.mutateAsync({ name: name.trim(), email: email.trim() });
      toast.success("Profile updated");
      setIsProfileOpen(false);
    } catch { toast.error("Failed to update profile"); }
  };

  const handleChangePassword = async () => {
    if (!currentPassword || !newPassword) return;
    try {
      await changePassword.mutateAsync({ currentPassword, newPassword });
      toast.success("Password changed");
      setCurrentPassword("");
      setNewPassword("");
      setIsPasswordOpen(false);
    } catch { toast.error("Failed to change password"); }
  };

  return (
    <header className="border-b border-border/50 bg-card/60 backdrop-blur-md sticky top-0 z-50 px-6 py-3">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-primary shadow-lg shadow-primary/20">
            <span className="text-lg font-black text-primary-foreground tracking-tighter">W</span>
          </div>
          <div>
            <h1 className="text-lg font-black text-foreground tracking-tight uppercase">Workspace Hub</h1>
            <p className="text-[10px] font-mono text-muted-foreground uppercase tracking-widest leading-none">Management Center</p>
          </div>
        </div>

        <div className="flex items-center gap-4">
          <div className="flex items-center gap-1 bg-muted/40 p-1 rounded-xl border border-border/30">
            <Button variant="ghost" size="icon" className="h-8 w-8 text-muted-foreground hover:text-foreground rounded-lg">
              <HelpCircle className="h-4 w-4" />
            </Button>
            <Button variant="ghost" size="icon" className="h-8 w-8 text-muted-foreground hover:text-foreground rounded-lg relative">
              <Bell className="h-4 w-4" />
              <div className="absolute top-2 right-2 h-1.5 w-1.5 bg-primary rounded-full border border-card" />
            </Button>
            
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" size="icon" className="h-8 w-8 text-muted-foreground hover:text-foreground rounded-lg">
                  <Settings className="h-4 w-4" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className="w-56 font-mono">
                <DropdownMenuLabel className="text-[10px] uppercase tracking-widest text-muted-foreground">Admin Menu</DropdownMenuLabel>
                <DropdownMenuSeparator />
                <DropdownMenuItem onClick={openProfile} className="text-xs uppercase tracking-wider font-bold">
                  <Pencil className="mr-2 h-3.5 w-3.5" /> Profile Settings
                </DropdownMenuItem>
                <DropdownMenuItem onClick={() => setIsPasswordOpen(true)} className="text-xs uppercase tracking-wider font-bold">
                  <KeyRound className="mr-2 h-3.5 w-3.5" /> Security
                </DropdownMenuItem>
                <DropdownMenuSeparator />
                <DropdownMenuItem onClick={logout} className="text-xs uppercase tracking-wider font-bold text-destructive hover:text-destructive">
                  <LogOut className="mr-2 h-3.5 w-3.5" /> Terminate Session
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>

          <div className="h-8 w-px bg-border/50 mx-2" />

          <div className="flex items-center gap-3 pl-2">
            <div className="text-right hidden sm:block">
              <p className="text-xs font-black text-foreground uppercase tracking-tighter leading-none">{user.name}</p>
              <p className="text-[9px] font-mono text-muted-foreground uppercase tracking-widest mt-1">Authorized</p>
            </div>
            <Avatar className="h-9 w-9 border-2 border-primary/20 shadow-md">
              <AvatarImage src="" />
              <AvatarFallback className="bg-primary/10 text-primary text-xs font-black uppercase">
                {user.name.substring(0, 2)}
              </AvatarFallback>
            </Avatar>
          </div>
        </div>
      </div>

      {/* Profile/Password Dialogs Integrated */}
      <Dialog open={isProfileOpen} onOpenChange={setIsProfileOpen}>
        <DialogContent className="max-w-sm">
          <DialogHeader>
            <DialogTitle className="font-black text-lg uppercase tracking-tight">Identity Profile</DialogTitle>
            <DialogDescription className="font-mono text-[10px] uppercase tracking-wider">Update your synchronization parameters.</DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-1">
               <p className="text-[10px] font-mono font-bold uppercase tracking-widest text-muted-foreground mb-1 ml-1">Core Name</p>
               <Input value={name} onChange={(e) => setName(e.target.value)} disabled={updateProfile.isPending} className="font-mono" />
            </div>
            <div className="space-y-1">
               <p className="text-[10px] font-mono font-bold uppercase tracking-widest text-muted-foreground mb-1 ml-1">E-Mail Address</p>
               <Input value={email} onChange={(e) => setEmail(e.target.value)} disabled={updateProfile.isPending} className="font-mono" />
            </div>
          </div>
          <DialogFooter>
            <Button onClick={() => void handleProfileSave()} disabled={updateProfile.isPending} className="w-full font-bold uppercase tracking-widest">Update Data</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={isPasswordOpen} onOpenChange={setIsPasswordOpen}>
        <DialogContent className="max-w-sm">
          <DialogHeader>
            <DialogTitle className="font-black text-lg uppercase tracking-tight">Access Protocol</DialogTitle>
            <DialogDescription className="font-mono text-[10px] uppercase tracking-wider">Secure your environment with a new key.</DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <Input type="password" placeholder="Current Key" value={currentPassword} onChange={(e) => setCurrentPassword(e.target.value)} disabled={changePassword.isPending} className="font-mono" />
            <Input type="password" placeholder="Future Key" value={newPassword} onChange={(e) => setNewPassword(e.target.value)} disabled={changePassword.isPending} className="font-mono" />
          </div>
          <DialogFooter>
            <Button onClick={() => void handleChangePassword()} disabled={changePassword.isPending} className="w-full font-bold uppercase tracking-widest">Commit Key</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </header>
  );
}

