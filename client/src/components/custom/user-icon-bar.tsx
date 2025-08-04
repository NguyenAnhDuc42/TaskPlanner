import { UserSummary } from "@/types/user";
import { UserSummaryType } from "./user-summary-type";

interface UserIconBarProps {
    users: UserSummary[];
    maxIcons?: number;
}

export function UserIconBar({ users, maxIcons = 5 }: UserIconBarProps) {
    const displayUsers = users.slice(0, maxIcons);
    const remainingUsers = users.length - displayUsers.length;

    return (
        <div className="border border-foreground rounded-lg p-2.5 bg-background hover:border-foreground transition-colors">
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