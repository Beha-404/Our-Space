import { MapPoint } from '../interfaces/memory-map';

export interface MapPlace {
    key: string;
    latitude: number;
    longitude: number;
    photos: MapPoint[];
}

const PLACE_PRECISION = 3;

export function groupIntoPlaces(points: MapPoint[]): MapPlace[] {
    const places = new Map<string, MapPlace>();

    for (const point of points) {
        const key = `${point.latitude.toFixed(PLACE_PRECISION)},${point.longitude.toFixed(PLACE_PRECISION)}`;
        const place = places.get(key);

        if (place) place.photos.push(point);
        else places.set(key, { key, latitude: point.latitude, longitude: point.longitude, photos: [point] });
    }

    return [...places.values()];
}
