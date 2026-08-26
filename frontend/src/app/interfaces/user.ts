export interface PartnerDto {
    id: number;
    username: string;
    profilePictureUrl: string | null;
    relationshipStartDate: string | null;
}

export type User = {
    id: number;
    username: string;
    email: string;
    profilePictureUrl: string | null;
    partner: PartnerDto | null;
    pendingEmail: string | null;
}
