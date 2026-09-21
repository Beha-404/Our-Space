import { MapPoint } from '../interfaces/memory-map';
import { clusterPlaces, PlaceOnScreen } from './map-clusters';
import { MapPlace } from './map-places';

function photo(id: number): MapPoint {
    return { id, latitude: 0, longitude: 0, thumbnailUrl: null, caption: null, date: '2026-01-01', locationName: null };
}

function onScreen(key: string, photos: number, x: number, y: number, latitude = 43, longitude = 18): PlaceOnScreen {
    const place: MapPlace = { key, latitude, longitude, photos: Array.from({ length: photos }, (_, i) => photo(i)) };
    return { place, x, y };
}

describe('clusterPlaces', () => {
    it('leaves a lone place as its own single pin', () => {
        const [only] = clusterPlaces([onScreen('a', 3, 100, 100)], 56);

        expect(only.places).toHaveLength(1);
        expect(only.key).toBe('p:a');
        expect(only.photoCount).toBe(3);
    });

    it('keeps places that are far apart on screen separate', () => {
        const clusters = clusterPlaces([onScreen('a', 1, 0, 0), onScreen('b', 1, 300, 0)], 56);

        expect(clusters).toHaveLength(2);
    });

    it('merges places that would overlap and adds up their photos', () => {
        const clusters = clusterPlaces([onScreen('a', 4, 100, 100), onScreen('b', 2, 120, 110)], 56);

        expect(clusters).toHaveLength(1);
        expect(clusters[0].places.map(p => p.key)).toEqual(['a', 'b']);
        expect(clusters[0].photoCount).toBe(6);
        expect(clusters[0].key.startsWith('c:6:')).toBe(true);
    });

    it('treats the radius edge as inside and just past it as outside', () => {
        expect(clusterPlaces([onScreen('a', 1, 0, 0), onScreen('b', 1, 56, 0)], 56)).toHaveLength(1);
        expect(clusterPlaces([onScreen('a', 1, 0, 0), onScreen('b', 1, 57, 0)], 56)).toHaveLength(2);
    });

    it('puts the cluster closer to the place with more photos', () => {
        const [cluster] = clusterPlaces([
            onScreen('a', 9, 0, 0, 40, 10),
            onScreen('b', 1, 10, 0, 50, 20),
        ], 56);

        expect(cluster.latitude).toBeCloseTo(41, 5);
        expect(cluster.longitude).toBeCloseTo(11, 5);
    });

    it('collapses a whole pile into one cluster', () => {
        const pile = Array.from({ length: 40 }, (_, i) => onScreen(`p${i}`, 1, 200 + (i % 5) * 4, 200 + (i % 7) * 3));
        const clusters = clusterPlaces(pile, 56);

        expect(clusters).toHaveLength(1);
        expect(clusters[0].photoCount).toBe(40);
    });

    it('gives the same cluster the same key when the map is only panned', () => {
        const before = clusterPlaces([onScreen('a', 2, 100, 100), onScreen('b', 3, 120, 100)], 56);
        const after = clusterPlaces([onScreen('a', 2, 340, 260), onScreen('b', 3, 360, 260)], 56);

        expect(after[0].key).toBe(before[0].key);
    });

    it('returns nothing for no places', () => {
        expect(clusterPlaces([], 56)).toEqual([]);
    });
});
