import { DecimalPipe } from '@angular/common';
import { Component, DestroyRef, effect, ElementRef, inject, input, output, signal, viewChild } from '@angular/core';
import * as L from 'leaflet';
import { TranslatePipe } from '../../i18n/translate.pipe';
import { TranslationService } from '../../i18n/translation.service';
import { NominatimPlace, PickedLocation, placeLabel, reversePlace, searchPlaces, toPickedLocation, uniqueByLabel } from './geocoding';

const TILE_URL = 'https://tile.openstreetmap.org/{z}/{x}/{y}.png';
const TILE_ATTRIBUTION = '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>';
const PICKED_ZOOM = 13;
const WORLD_BOUNDS: L.LatLngBoundsExpression = [[-90, -180], [90, 180]];
const HOME_VIEW: Record<string, { center: L.LatLngTuple; zoom: number }> = {
  bs: { center: [44.0, 17.8], zoom: 6 },
};
const DEFAULT_VIEW = { center: [46, 12] as L.LatLngTuple, zoom: 4 };

type SearchState = 'idle' | 'searching' | 'empty' | 'failed';

@Component({
  selector: 'app-location-picker',
  imports: [TranslatePipe, DecimalPipe],
  templateUrl: './location-picker.html',
  styleUrl: './location-picker.css',
})
export class LocationPicker {
  private i18n = inject(TranslationService);

  value = input<PickedLocation | null>(null);
  valueChange = output<PickedLocation | null>();

  expanded = signal(false);
  query = signal('');
  results = signal<NominatimPlace[]>([]);
  searchState = signal<SearchState>('idle');

  private mapElement = viewChild<ElementRef<HTMLElement>>('mapElement');
  private map: L.Map | null = null;
  private marker: L.Marker | null = null;
  private resizeObserver: ResizeObserver | null = null;
  private searchAbort: AbortController | null = null;
  private reverseAbort: AbortController | null = null;

  constructor() {
    effect(() => {
      const element = this.mapElement();
      if (element && !this.map) this.createMap(element.nativeElement);
      if (!element && this.map) this.destroyMap();
    });

    inject(DestroyRef).onDestroy(() => {
      this.searchAbort?.abort();
      this.reverseAbort?.abort();
      this.destroyMap();
    });
  }

  label(place: NominatimPlace): string {
    return placeLabel(place);
  }

  expand(): void {
    this.expanded.set(true);
  }

  collapse(): void {
    this.expanded.set(false);
    this.results.set([]);
    this.searchState.set('idle');
  }

  remove(): void {
    this.valueChange.emit(null);
    this.marker?.remove();
    this.marker = null;
  }

  async search(): Promise<void> {
    const query = this.query().trim();
    if (!query) return;

    this.searchAbort?.abort();
    this.searchAbort = new AbortController();
    this.searchState.set('searching');

    try {
      const places = uniqueByLabel(await searchPlaces(query, this.i18n.lang(), this.searchAbort.signal));
      this.results.set(places);
      this.searchState.set(places.length ? 'idle' : 'empty');
    } catch (error) {
      if ((error as Error).name === 'AbortError') return;
      this.results.set([]);
      this.searchState.set('failed');
    }
  }

  choose(place: NominatimPlace): void {
    const picked = toPickedLocation(place);
    this.results.set([]);
    this.valueChange.emit(picked);
    this.placeMarker(picked.latitude, picked.longitude);
    this.map?.setView([picked.latitude, picked.longitude], PICKED_ZOOM);
  }

  private createMap(element: HTMLElement): void {
    const current = this.value();
    const home = HOME_VIEW[this.i18n.lang()] ?? DEFAULT_VIEW;

    const map = L.map(element, {
      zoomControl: true,
      maxBounds: WORLD_BOUNDS,
      maxBoundsViscosity: 1,
    });
    this.map = map;
    map.attributionControl.setPrefix(false);
    L.tileLayer(TILE_URL, { attribution: TILE_ATTRIBUTION, maxZoom: 19, noWrap: true }).addTo(map);

    if (current) {
      map.setView([current.latitude, current.longitude], PICKED_ZOOM);
      this.placeMarker(current.latitude, current.longitude);
    } else {
      map.setView(home.center, home.zoom);
    }

    map.on('click', event => this.pickOnMap(event.latlng.lat, event.latlng.lng));

    this.resizeObserver = new ResizeObserver(() => map.invalidateSize());
    this.resizeObserver.observe(element);
  }

  private destroyMap(): void {
    this.resizeObserver?.disconnect();
    this.resizeObserver = null;
    this.map?.remove();
    this.map = null;
    this.marker = null;
  }

  private placeMarker(latitude: number, longitude: number): void {
    if (!this.map) return;

    if (this.marker) {
      this.marker.setLatLng([latitude, longitude]);
      return;
    }

    const icon = L.divIcon({ className: 'picker-pin', iconSize: [22, 22], iconAnchor: [11, 11] });
    this.marker = L.marker([latitude, longitude], { icon, draggable: true }).addTo(this.map);
    this.marker.on('dragend', () => {
      const position = this.marker!.getLatLng();
      this.pickOnMap(position.lat, position.lng);
    });
  }

  private async pickOnMap(latitude: number, longitude: number): Promise<void> {
    const lat = Math.round(latitude * 10000) / 10000;
    const lon = Math.round(L.Util.wrapNum(longitude, [-180, 180], true) * 10000) / 10000;

    this.placeMarker(lat, lon);
    this.valueChange.emit({ latitude: lat, longitude: lon, name: null });

    this.reverseAbort?.abort();
    this.reverseAbort = new AbortController();

    try {
      const place = await reversePlace(lat, lon, this.i18n.lang(), this.reverseAbort.signal);
      const current = this.value();
      if (place && current?.latitude === lat && current.longitude === lon)
        this.valueChange.emit({ latitude: lat, longitude: lon, name: placeLabel(place) });
    } catch {
      return;
    }
  }
}
