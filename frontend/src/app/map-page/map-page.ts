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
import { clusterPlaces, MapCluster } from './map-clusters';
import { groupIntoPlaces, MapPlace } from './map-places';

const CLUSTER_RADIUS_PX = 60;
const CLUSTER_SPLIT_MAX_ZOOM = 18;
const CLUSTER_SPLIT_PADDING: L.PointExpression = [80, 80];
const TILE_URL = 'https://tile.openstreetmap.org/{z}/{x}/{y}.png';
const TILE_ATTRIBUTION = '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>';
const SINGLE_PLACE_ZOOM = 13;
const PANEL_OFFSET_PX = 90;
const WORLD_BOUNDS: L.LatLngBoundsExpression = [[-90, -180], [90, 180]];

@Component({
  imports: [LocalDatePipe, TranslatePipe, Skeleton, RouterLink],
  selector: 'app-map-page',
  styleUrl: './map-page.css',
  templateUrl: './map-page.html',
  providers: [LocalDatePipe],
})
export class MapPage {
  private mapService = inject(MemoryMapService);
  private photoService = inject(PhotoService);
  private userService = inject(UserService);
  private i18n = inject(TranslationService);
  private localDate = inject(LocalDatePipe);

  private mapElement = viewChild<ElementRef<HTMLElement>>('mapElement');
  private map: L.Map | null = null;
  private layer: L.LayerGroup | null = null;
  private markers = new Map<string, L.Marker>();
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
    const map = L.map(element, {
      zoomControl: false,
      attributionControl: true,
      maxBounds: WORLD_BOUNDS,
      maxBoundsViscosity: 1,
    });
    this.map = map;

    L.control.zoom({ position: 'topright' }).addTo(map);
    map.attributionControl.setPrefix('<a href="https://leafletjs.com">Leaflet</a>');
    L.tileLayer(TILE_URL, { attribution: TILE_ATTRIBUTION, maxZoom: 19, noWrap: true }).addTo(map);

    this.layer = L.layerGroup().addTo(map);
    map.on('moveend', () => this.renderMarkers());

    this.fitToPlaces(places);
    this.renderMarkers();

    let fitted = false;
    this.resizeObserver = new ResizeObserver(() => {
      map.invalidateSize();
      if (!fitted && element.clientWidth > 0) {
        fitted = true;
        this.fitToPlaces(places);
        this.renderMarkers();
      }
    });
    this.resizeObserver.observe(element);
  }

  private renderMarkers(): void {
    const map = this.map;
    const layer = this.layer;
    if (!map || !layer) return;

    const items = this.places().map(place => {
      const point = map.latLngToContainerPoint([place.latitude, place.longitude]);
      return { place, x: point.x, y: point.y };
    });

    const view = map.getBounds().pad(0.3);
    const wanted = new Map<string, MapCluster>();
    for (const cluster of clusterPlaces(items, CLUSTER_RADIUS_PX)) {
      if (view.contains([cluster.latitude, cluster.longitude])) wanted.set(cluster.key, cluster);
    }

    for (const [key, marker] of this.markers) {
      if (wanted.has(key)) continue;
      layer.removeLayer(marker);
      this.markers.delete(key);
    }

    for (const [key, cluster] of wanted) {
      if (this.markers.has(key)) continue;

      const marker = cluster.places.length === 1
        ? this.createMarker(cluster.places[0])
        : this.createClusterMarker(cluster);
      layer.addLayer(marker);
      this.markers.set(key, marker);
    }
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

  private clusterAsPlace(cluster: MapCluster): MapPlace {
    return {
      key: cluster.key,
      latitude: cluster.latitude,
      longitude: cluster.longitude,
      photos: cluster.places.flatMap(place => place.photos),
    };
  }

  private clusterName(cluster: MapCluster): string | null {
    const names = [...new Set(cluster.places.map(place => this.placeName(place)).filter((name): name is string => !!name))];
    if (names.length === 0) return null;

    return names.length === 1 ? names[0] : `${names[0]} +${names.length - 1}`;
  }

  private openCluster(cluster: MapCluster): void {
    const map = this.map;
    if (!map) return;

    const bounds = L.latLngBounds(cluster.places.map(place => [place.latitude, place.longitude]));
    const targetZoom = map.getBoundsZoom(bounds, false, L.point(CLUSTER_SPLIT_PADDING));

    if (targetZoom > map.getZoom() && map.getZoom() < CLUSTER_SPLIT_MAX_ZOOM) {
      map.fitBounds(bounds, { padding: CLUSTER_SPLIT_PADDING, maxZoom: CLUSTER_SPLIT_MAX_ZOOM });
      return;
    }

    const place = this.clusterAsPlace(cluster);
    this.selectedPlace.set(place);
    this.centerAbovePanel(place);
  }

  private createClusterMarker(cluster: MapCluster): L.Marker {
    const place = this.clusterAsPlace(cluster);
    const cover = [...place.photos].filter(photo => photo.thumbnailUrl).sort((a, b) => b.date.localeCompare(a.date))[0];

    const root = document.createElement('div');
    root.className = 'map-cluster';

    const card = document.createElement('div');
    card.className = 'map-cluster-card';
    if (cover?.thumbnailUrl) {
      const image = document.createElement('img');
      image.src = this.thumbnail(cover.thumbnailUrl);
      image.alt = '';
      card.appendChild(image);
    }
    root.appendChild(card);

    const count = document.createElement('span');
    count.className = 'map-cluster-count';
    count.textContent = String(cluster.photoCount);
    root.appendChild(count);

    const icon = L.divIcon({ html: root, className: 'map-pin-wrap', iconSize: [60, 66], iconAnchor: [30, 64] });

    return L.marker([cluster.latitude, cluster.longitude], { icon, keyboard: true })
      .bindTooltip(() => this.tooltipFor(place, this.clusterName(cluster), this.i18n.t('map.clusterHint')), {
        direction: 'right',
        offset: [32, -32],
        className: 'pin-tooltip',
        opacity: 1,
      })
      .on('click', () => this.openCluster(cluster));
  }

  private tooltipFor(place: MapPlace, name: string | null = this.placeName(place), hint: string | null = null): HTMLElement {
    const photos = place.photos;
    const cover = photos[0];
    const root = document.createElement('div');

    const title = document.createElement('strong');
    title.textContent = photos.length === 1
      ? cover.caption || this.i18n.t('memories.untitledPhoto')
      : `${photos.length} ${this.i18n.t(this.photosKey(photos.length))}`;
    root.appendChild(title);

    if (name) {
      const location = document.createElement('span');
      location.className = 'pin-tooltip-place';
      location.textContent = name;
      root.appendChild(location);
    }

    const dates = photos.map(photo => photo.date).sort();
    const first = this.localDate.transform(dates[0], 'longDate');
    const last = this.localDate.transform(dates[dates.length - 1], 'longDate');
    const date = document.createElement('span');
    date.className = 'pin-tooltip-date';
    date.textContent = first === last ? first : `${first} – ${last}`;
    root.appendChild(date);

    if (hint) {
      const hintElement = document.createElement('span');
      hintElement.className = 'pin-tooltip-hint';
      hintElement.textContent = hint;
      root.appendChild(hintElement);
    }

    return root;
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
      .bindTooltip(() => this.tooltipFor(place), {
        direction: 'right',
        offset: [30, -30],
        className: 'pin-tooltip',
        opacity: 1,
      })
      .on('click', () => {
        this.selectedPlace.set(place);
        this.centerAbovePanel(place);
      });
  }
}
