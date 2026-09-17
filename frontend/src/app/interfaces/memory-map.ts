export interface MapPoint {
    id: number;
    latitude: number;
    longitude: number;
    thumbnailUrl: string | null;
    caption: string | null;
    date: string;
}

export interface MemoryMap {
    points: MapPoint[];
    photosWithoutLocation: number;
}
