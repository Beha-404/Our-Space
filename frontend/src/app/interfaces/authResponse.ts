export interface AuthResponse {
    token: string;
    refreshToken: string;
    expiresAt: string;
    userId: number;
    username: string;
    email: string;
}
