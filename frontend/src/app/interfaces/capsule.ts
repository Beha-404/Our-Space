export interface Capsule {
    id: number;
    title: string;
    message: string | null;
    openAt: string | null;
    openedAt: string | null;
    isUnlocked: boolean;
    canOpenNow: boolean;
    createdByUsername: string;
    createdAt: string;
}

export interface CreateCapsuleRequest {
    title: string;
    message: string;
    openAt: string | null;
}
