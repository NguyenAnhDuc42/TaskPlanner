import { UserSummary } from "@/types/user";

import { Avatar, AvatarFallback } from "../ui/avatar";
import { RoleBadge } from "./role-badge";
import { mapRoleToAvatarStyle } from "@/utils/role-utils";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "../ui/tooltip";

interface UserSummaryTypeProps {
    userSummary: UserSummary;
    styleDisplay?: "card" | "icon";
}

const getInitials = (name: string) => {
    return name
        .split(' ')
        .map(word => word.charAt(0))
        .join('')
        .toUpperCase()
        .slice(0, 2);
};

export function UserSummaryType({ userSummary, styleDisplay = "card" }: UserSummaryTypeProps) {
    const avatarClasses = mapRoleToAvatarStyle(userSummary.role);

    if (styleDisplay === "icon") {
        return (
            <TooltipProvider>
                <Tooltip>
                    <TooltipTrigger asChild>
                        <Avatar className="h-8 w-8 border-2 border-white dark:border-gray-800">
                            <AvatarFallback className={`text-xs font-bold ${avatarClasses}`}>
                                {getInitials(userSummary.name)}
                            </AvatarFallback>
                        </Avatar>
                    </TooltipTrigger>
                    <TooltipContent>
                        <p>{userSummary.name}</p>
                    </TooltipContent>
                </Tooltip>
            </TooltipProvider>
        );
    }

    if (styleDisplay === "card") {
        return (
            <div className="flex items-center gap-3 py-2">
                <Avatar className="h-10 w-10">
                    <AvatarFallback className={`text-sm font-bold ${avatarClasses}`}>
                        {getInitials(userSummary.name)}
                    </AvatarFallback>
                </Avatar>
                <div className="flex-1 min-w-0">
                    <p className="truncate font-semibold text-gray-900 dark:text-white">{userSummary.name}</p>
                    <div className="truncate text-sm text-gray-500 dark:text-gray-400">
                        <RoleBadge role={userSummary.role} size="sm" />
                    </div>
                </div>
            </div>
        );
    }

    return null;
}