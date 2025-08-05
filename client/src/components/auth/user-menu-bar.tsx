import { UserDetail } from "@/types/user";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuLabel, DropdownMenuSeparator, DropdownMenuTrigger } from "../ui/dropdown-menu";
import { Button } from "../ui/button";
import { Avatar, AvatarFallback } from "../ui/avatar";
import { ChevronDown } from "lucide-react";
import { cn } from "@/lib/utils";


interface UserMenuProps {
  currentUser: UserDetail;
  isCollapsed?: boolean;
  onProfileSettings?: () => void;
  onNotifications?: () => void;
  onBilling?: () => void;
  onSignOut?: () => void;
  className?: string;
}

export function UserMenuBar({ currentUser, isCollapsed, onProfileSettings, onNotifications, onBilling, onSignOut, className }: UserMenuProps) {
  const getInitials = (name: string) => {
    return name
      .split(' ')
      .map(word => word.charAt(0))
      .join('')
      .toUpperCase()
      .slice(0, 2);
  };

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="ghost"
          className={cn(
            "flex items-center h-full w-full text-white hover:bg-gray-900",
            isCollapsed ? "justify-center" : "justify-between",
            className
          )}
        >
          <div className="flex items-center gap-3">
            <Avatar className="h-8 w-8 ring-1 ring-gray-700">
              <AvatarFallback className="bg-white text-black text-sm">
                {getInitials(currentUser.name)}
              </AvatarFallback>
            </Avatar>
            {!isCollapsed && (
              <div className="hidden md:block text-left">
                <p className="text-sm text-white truncate max-w-32">{currentUser.name}</p>
                <p className="text-xs text-gray-400 truncate max-w-32">{currentUser.email}</p>
              </div>
            )}
          </div>
          {!isCollapsed && <ChevronDown className="h-4 w-4 text-gray-400" />}
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-56 bg-gray-900 border-gray-700">
        <DropdownMenuLabel className="text-white">
          <div className="flex flex-col space-y-1">
            <p className="text-sm">{currentUser.name}</p>
            <p className="text-xs text-gray-400">{currentUser.email}</p>
          </div>
        </DropdownMenuLabel>
        <DropdownMenuSeparator className="bg-gray-700" />
        <DropdownMenuItem
          className="text-gray-200 focus:bg-gray-800 focus:text-white"
          onClick={onProfileSettings}
        >
          Profile Settings
        </DropdownMenuItem>
        <DropdownMenuItem
          className="text-gray-200 focus:bg-gray-800 focus:text-white"
          onClick={onNotifications}
        >
          Notifications
        </DropdownMenuItem>
        <DropdownMenuItem
          className="text-gray-200 focus:bg-gray-800 focus:text-white"
          onClick={onBilling}
        >
          Billing
        </DropdownMenuItem>
        <DropdownMenuSeparator className="bg-gray-700" />
        <DropdownMenuItem
          className="text-red-400 focus:bg-red-900/20 focus:text-red-300"
          onClick={onSignOut}
        >
          Sign Out
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}