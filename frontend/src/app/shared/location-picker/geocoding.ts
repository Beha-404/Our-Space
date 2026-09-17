export interface PickedLocation {
  latitude: number;
  longitude: number;
  name: string | null;
}

export interface NominatimPlace {
  lat: string;
  lon: string;
  name?: string;
  display_name: string;
  address?: Record<string, string | undefined>;
}

const NOMINATIM_URL = 'https://nominatim.openstreetmap.org';
const MAX_RESULTS = 5;
const PLACE_KEYS = ['city', 'town', 'village', 'hamlet', 'suburb', 'municipality', 'county', 'state'];

export function placeLabel(place: NominatimPlace): string {
  const address = place.address ?? {};
  const locality = place.name || PLACE_KEYS.map(key => address[key]).find(Boolean);
  const country = address['country'];

  if (!locality) return place.display_name.split(',').slice(0, 2).join(',').trim();
  return country && country !== locality ? `${locality}, ${country}` : locality;
}

export function toPickedLocation(place: NominatimPlace): PickedLocation {
  return {
    latitude: Number(place.lat),
    longitude: Number(place.lon),
    name: placeLabel(place),
  };
}

export async function searchPlaces(query: string, lang: string, signal?: AbortSignal): Promise<NominatimPlace[]> {
  const params = new URLSearchParams({
    q: query,
    format: 'jsonv2',
    addressdetails: '1',
    limit: String(MAX_RESULTS),
    'accept-language': lang,
  });

  const response = await fetch(`${NOMINATIM_URL}/search?${params}`, { signal });
  if (!response.ok) throw new Error(`Geocoding failed: ${response.status}`);
  return response.json();
}

export async function reversePlace(latitude: number, longitude: number, lang: string, signal?: AbortSignal): Promise<NominatimPlace | null> {
  const params = new URLSearchParams({
    lat: String(latitude),
    lon: String(longitude),
    format: 'jsonv2',
    zoom: '12',
    addressdetails: '1',
    'accept-language': lang,
  });

  const response = await fetch(`${NOMINATIM_URL}/reverse?${params}`, { signal });
  if (!response.ok) return null;

  const place = await response.json();
  return place?.error ? null : place;
}

export function uniqueByLabel(places: NominatimPlace[]): NominatimPlace[] {
  const seen = new Set<string>();
  return places.filter(place => {
    const label = placeLabel(place);
    if (seen.has(label)) return false;
    seen.add(label);
    return true;
  });
}
