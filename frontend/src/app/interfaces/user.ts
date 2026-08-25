export interface PartnerDto {
    id: number;
    username: string;
    displayName: string | null;
    profilePictureUrl: string | null;
    relationshipStartDate: string | null;
}

export type User = {
    id: number;
    username: string;
    email: string;
    displayName: string | null;
    profilePictureUrl: string | null;
    partner: PartnerDto | null;
}
