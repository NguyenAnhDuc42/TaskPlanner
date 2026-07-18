import { UserAvatar } from "@/components/user-avatar";
import { useLogout, useUser } from "@/features/auth/auth-api";
import { LogOut, User } from "lucide-react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";

export function UserMenu({ onOpenProfile }: { readonly onOpenProfile: () => void }) {
  const { data: user } = useUser();
  const { mutate: logout } = useLogout();

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <button type="button" className="outline-none cursor-pointer hover:opacity-80 transition-opacity shrink-0">
          <UserAvatar
            name={user?.name || "User"}
            avatarUrl={null}
            className="h-6.5 w-6.5 rounded-md"
            fallbackClassName="text-[9px] rounded-md shadow-sm"
          />
        </button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-52 p-0 overflow-hidden">
        <DropdownMenuLabel className="px-2 py-1.5">
          <p className="text-xs font-bold text-foreground/90 truncate">{user?.name ?? "User"}</p>
          <p className="text-[10px] text-muted-foreground/50 font-medium truncate">{user?.email ?? ""}</p>
        </DropdownMenuLabel>
        <DropdownMenuSeparator className="bg-border m-0" />
        <DropdownMenuItem
          className="flex items-center gap-2 px-3 py-2 text-xs font-medium text-muted-foreground/70 hover:text-foreground cursor-pointer rounded-none"
          onClick={onOpenProfile}
        >
          <User className="h-3.5 w-3.5" />
          Profile
        </DropdownMenuItem>
        <DropdownMenuSeparator className="bg-border m-0" />
        <DropdownMenuItem
          className="flex items-center gap-2 px-3 py-2 text-xs font-medium text-destructive/80 hover:text-destructive hover:bg-destructive/10 cursor-pointer rounded-none"
          onClick={() => logout()}
        >
          <LogOut className="h-3.5 w-3.5" />
          Sign out
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
