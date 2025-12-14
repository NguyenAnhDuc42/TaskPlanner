export enum Role {
    Owner = 'Owner',
    Admin = 'Admin',
    Member = 'Member',
    Guest = 'Guest'
}

export function mapRoleFromApi(role: number): Role {
    switch (role) {
        case 0:
            return Role.Owner;
        case 1:
            return Role.Admin;
        case 2:
            return Role.Member;
        case 3:
            return Role.Guest;
        default:
            return Role.Guest;
    }
}

export function mapRoleToApi(role: Role): number {
    switch (role) {
        case Role.Owner:
            return 0;
        case Role.Admin:
            return 1;
        case Role.Member:
            return 2;
        case Role.Guest:
            return 3;
    }
}

export interface BadgeContext {
    roleName: string;
    badgeClasses: string;
}

export function mapRoleToBadge(role: Role | null): BadgeContext {
    switch (role) {
        case Role.Owner:
            return {
                roleName: 'Owner',
                badgeClasses: 'bg-yellow-400 text-yellow-900 hover:bg-yellow-500'
            };
        case Role.Admin:
            return {
                roleName: 'Admin',
                badgeClasses: 'bg-red-500 text-white hover:bg-red-600'
            };
        case Role.Member:
            return {
                roleName: 'Member',
                badgeClasses: 'bg-blue-500 text-white hover:bg-blue-600'
            };
        case Role.Guest:
            return {
                roleName: 'Guest',
                badgeClasses: 'bg-gray-400 text-gray-900 hover:bg-gray-500'
            };
        default:
            return {
                roleName: 'Unknown',
                badgeClasses: 'bg-gray-200 text-gray-800'
            };
    }
}

export function mapRoleToAvatarStyle(role: Role | null): string {
    switch (role) {
        case Role.Owner:
            return "bg-yellow-400 text-yellow-900";
        case Role.Admin:
            return "bg-red-500 text-white";
        case Role.Member:
            return "bg-blue-500 text-white";
        case Role.Guest:
            return "bg-gray-400 text-gray-900";
        default:
            return "bg-gray-200 text-gray-800";
    }
}