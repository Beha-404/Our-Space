export interface Photo {
    id: number;
    url: string;
    thumbnailUrl: string;
    caption: string | null;
    takenAt: string;
    uploadedByUsername: string;
    createdAt: string;
}
