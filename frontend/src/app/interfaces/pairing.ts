export interface PairingCodeResponse {
    code: string;
    expiresAt: string;
}

export interface PairRequest {
    code: string;
    relationshipStartDate: string | null;
}
