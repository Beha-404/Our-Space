export interface AuthResponse {
    token: string;
    expiresAt: string;
    userId: number;
    username: string;
    email: string;
}

export interface LoginResponse {
    requiresTwoFactor: boolean;
    auth: AuthResponse | null;
}
