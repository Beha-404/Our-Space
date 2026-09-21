import { MapPlace } from './map-places';

export interface PlaceOnScreen {
    place: MapPlace;
    x: number;
    y: number;
}

export interface MapCluster {
    key: string;
    latitude: number;
    longitude: number;
    places: MapPlace[];
    photoCount: number;
}

interface Group {
    x: number;
    y: number;
    weight: number;
    items: PlaceOnScreen[];
}

export function clusterPlaces(items: PlaceOnScreen[], radiusPx: number): MapCluster[] {
    const radiusSquared = radiusPx * radiusPx;
    const groups: Group[] = [];

    for (const item of items) {
        const weight = item.place.photos.length;
        const group = groups.find(g => (g.x - item.x) ** 2 + (g.y - item.y) ** 2 <= radiusSquared);

        if (group) {
            const total = group.weight + weight;
            group.x = (group.x * group.weight + item.x * weight) / total;
            group.y = (group.y * group.weight + item.y * weight) / total;
            group.weight = total;
            group.items.push(item);
        } else {
            groups.push({ x: item.x, y: item.y, weight, items: [item] });
        }
    }

    return groups.map(toCluster);
}

function toCluster(group: Group): MapCluster {
    const places = group.items.map(item => item.place);

    if (places.length === 1) {
        const [place] = places;
        return { key: `p:${place.key}`, latitude: place.latitude, longitude: place.longitude, places, photoCount: place.photos.length };
    }

    let latitude = 0;
    let longitude = 0;
    for (const place of places) {
        latitude += place.latitude * place.photos.length;
        longitude += place.longitude * place.photos.length;
    }
    latitude /= group.weight;
    longitude /= group.weight;

    return {
        key: `c:${group.weight}:${latitude.toFixed(4)},${longitude.toFixed(4)}`,
        latitude,
        longitude,
        places,
        photoCount: group.weight,
    };
}
