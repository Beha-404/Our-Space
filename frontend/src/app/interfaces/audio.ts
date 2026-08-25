export interface AudioMessage {
    id: number;
    url: string;
    caption: string | null;
    recordedAt: string;
    uploadedByUsername: string;
    createdAt: string;
}
