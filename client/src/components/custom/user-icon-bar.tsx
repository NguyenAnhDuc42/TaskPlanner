import { UserSummary } from "@/types/user";
import { UserSummaryType } from "./user-summary-type";
import { cn } from "@/lib/utils";

interface UserIconBarProps {
    users: UserSummary[];
    maxIcons?: number;
    className?: string;
}

export function UserIconBar({ users, maxIcons = 5, className }: UserIconBarProps) {
    const displayUsers = users.slice(0, maxIcons);
    const remainingUsers = users.length - displayUsers.length;

    return (
        <div className={cn("border rounded-lg p-2.5 bg-background hover:border-foreground transition-colors", className)}>
            <div className="flex items-center gap-2">
                {displayUsers.map((user) => (
                    <UserSummaryType key={user.id} userSummary={user} styleDisplay="icon" />
                ))}
                {remainingUsers > 0 && (
                    <div className="flex items-center justify-center h-6 w-6 bg-white text-black rounded-full flex-shrink-0">
                        <span className="text-xs">
                            +{remainingUsers}
                        </span>
                    </div>
                )}
            </div>
        </div>
    );
}