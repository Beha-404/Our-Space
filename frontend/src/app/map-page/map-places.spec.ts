import { MapPoint } from '../interfaces/memory-map';
import { groupIntoPlaces } from './map-places';

function point(id: number, latitude: number, longitude: number): MapPoint {
    return { id, latitude, longitude, thumbnailUrl: null, caption: null, date: '2026-01-01' };
}

describe('groupIntoPlaces', () => {
    it('puts photos taken at the same spot into one place', () => {
        const places = groupIntoPlaces([point(1, 43.8563, 18.4131), point(2, 43.8564, 18.4132)]);

        expect(places).toHaveLength(1);
        expect(places[0].photos.map(p => p.id)).toEqual([1, 2]);
    });

    it('keeps photos from different spots apart', () => {
        const places = groupIntoPlaces([point(1, 43.8563, 18.4131), point(2, 45.8150, 15.9819)]);

        expect(places).toHaveLength(2);
    });

    it('returns nothing for no photos', () => {
        expect(groupIntoPlaces([])).toEqual([]);
    });
});
