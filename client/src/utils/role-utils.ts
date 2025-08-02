export enum Role {
    Owner = 'owner',
    Admin = 'admin',
    Member = 'member',
    Guest = 'guest'
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