import { Component, computed, DestroyRef, effect, ElementRef, inject, signal, viewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import * as L from 'leaflet';
import { LocalDatePipe } from '../i18n/local-date.pipe';
import { TranslatePipe } from '../i18n/translate.pipe';
import { TranslationService } from '../i18n/translation.service';
import { MemoryMap } from '../interfaces/memory-map';
import { MemoryMapService } from '../services/memory-map.service';
import { PhotoService } from '../services/photo.service';
import { UserService } from '../services/user.service';
import { pluralKey } from '../shared/plural';
import { Skeleton } from '../shared/skeleton/skeleton';
import { groupIntoPlaces, MapPlace } from './map-places';

const TILE_URL = 'https://tile.openstreetmap.org/{z}/{x}/{y}.png';
const TILE_ATTRIBUTION = '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>';
const SINGLE_PLACE_ZOOM = 13;
const PANEL_OFFSET_PX = 90;

@Component({
  imports: [LocalDatePipe, TranslatePipe, Skeleton, RouterLink],
  selector: 'app-map-page',
  styleUrl: './map-page.css',
  templateUrl: './map-page.html',
})
export class MapPage {
  private mapService = inject(MemoryMapService);
  private photoService = inject(PhotoService);
  private userService = inject(UserService);
  private i18n = inject(TranslationService);

  private mapElement = viewChild<ElementRef<HTMLElement>>('mapElement');
  private map: L.Map | null = null;
  private resizeObserver: ResizeObserver | null = null;

  userLoaded = computed(() => !!this.userService.currentUser());
  isPaired = computed(() => !!this.userService.currentUser()?.partner);

  data = signal<MemoryMap | null>(null);
  loading = signal(true);
  selectedPlace = signal<MapPlace | null>(null);

  places = computed(() => groupIntoPlaces(this.data()?.points ?? []));
  hasPoints = computed(() => this.places().length > 0);
  withoutLocation = computed(() => this.data()?.photosWithoutLocation ?? 0);

  constructor() {
    this.userService.ensureCurrentUser().subscribe(user => {
      if (user.partner) this.load();
      else this.loading.set(false);
    });

    effect(() => {
      const element = this.mapElement();
      const places = this.places();
      if (element && places.length > 0 && !this.map)
        this.createMap(element.nativeElement, places);
    });

    inject(DestroyRef).onDestroy(() => {
      this.resizeObserver?.disconnect();
      this.map?.remove();
    });
  }

  thumbnail(url: string | null): string {
    return url ? this.photoService.fullUrl(url) : '';
  }

  photosKey(count: number): string {
    return pluralKey('map.photos', count, this.i18n.lang());
  }

  placeName(place: MapPlace): string | null {
    return place.photos.find(photo => photo.locationName)?.locationName ?? null;
  }

  closePlace(): void {
    this.selectedPlace.set(null);
  }

  private load(): void {
    this.mapService.get().subscribe({
      next: data => {
        this.data.set(data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  private createMap(element: HTMLElement, places: MapPlace[]): void {
    const map = L.map(element, { zoomControl: false, attributionControl: true, worldCopyJump: true });
    this.map = map;

    L.control.zoom({ position: 'topright' }).addTo(map);
    map.attributionControl.setPrefix('<a href="https://leafletjs.com">Leaflet</a>');
    L.tileLayer(TILE_URL, { attribution: TILE_ATTRIBUTION, maxZoom: 19 }).addTo(map);

    for (const place of places)
      this.createMarker(place).addTo(map);

    this.fitToPlaces(places);

    let fitted = false;
    this.resizeObserver = new ResizeObserver(() => {
      map.invalidateSize();
      if (!fitted && element.clientWidth > 0) {
        fitted = true;
        this.fitToPlaces(places);
      }
    });
    this.resizeObserver.observe(element);
  }

  private fitToPlaces(places: MapPlace[]): void {
    const bounds = L.latLngBounds(places.map(place => [place.latitude, place.longitude]));

    if (places.length === 1) this.map?.setView(bounds.getCenter(), SINGLE_PLACE_ZOOM);
    else this.map?.fitBounds(bounds, { paddingTopLeft: [40, 80], paddingBottomRight: [40, 24], maxZoom: 15 });
  }

  private centerAbovePanel(place: MapPlace): void {
    const map = this.map;
    if (!map) return;

    const target = map.project([place.latitude, place.longitude]).add([0, PANEL_OFFSET_PX]);
    map.panTo(map.unproject(target));
  }

  private createMarker(place: MapPlace): L.Marker {
    const cover = place.photos[0];
    const pin = document.createElement('div');
    pin.className = 'map-pin';

    if (cover.thumbnailUrl) {
      const image = document.createElement('img');
      image.src = this.thumbnail(cover.thumbnailUrl);
      image.alt = '';
      pin.appendChild(image);
    }

    if (place.photos.length > 1) {
      const count = document.createElement('span');
      count.className = 'map-pin-count';
      count.textContent = String(place.photos.length);
      pin.appendChild(count);
    }

    const icon = L.divIcon({ html: pin, className: 'map-pin-wrap', iconSize: [52, 58], iconAnchor: [26, 58] });

    return L.marker([place.latitude, place.longitude], { icon, keyboard: true })
      .on('click', () => {
        this.selectedPlace.set(place);
        this.centerAbovePanel(place);
      });
  }
}
