export interface Photo {
    id: number;
    url: string;
    thumbnailUrl: string;
    mediumUrl: string | null;
    caption: string | null;
    takenAt: string;
    uploadedByUsername: string;
    createdAt: string;
}
