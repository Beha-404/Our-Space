export interface MapPoint {
    id: number;
    latitude: number;
    longitude: number;
    thumbnailUrl: string | null;
    caption: string | null;
    date: string;
    locationName: string | null;
}

export interface MemoryMap {
    points: MapPoint[];
    photosWithoutLocation: number;
}
