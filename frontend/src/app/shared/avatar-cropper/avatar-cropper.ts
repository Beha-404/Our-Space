import { Component, computed, DestroyRef, ElementRef, inject, input, OnInit, output, signal, viewChild } from '@angular/core';
import { TranslatePipe } from '../../i18n/translate.pipe';

const VIEWPORT_SIZE = 240;
const OUTPUT_SIZE = 500;
const MIN_ZOOM = 1;
const MAX_ZOOM = 3;

@Component({
  imports: [TranslatePipe],
  selector: 'app-avatar-cropper',
  styleUrl: './avatar-cropper.css',
  templateUrl: './avatar-cropper.html',
})
export class AvatarCropper implements OnInit {
  private destroyRef = inject(DestroyRef);

  file = input.required<File>();
  cropped = output<Blob>();
  cancelled = output<void>();

  imageRef = viewChild<ElementRef<HTMLImageElement>>('image');

  readonly viewportSize = VIEWPORT_SIZE;

  imageUrl = signal('');
  naturalWidth = signal(0);
  naturalHeight = signal(0);
  imageReady = computed(() => this.naturalWidth() > 0 && this.naturalHeight() > 0);

  zoom = signal(MIN_ZOOM);
  offsetX = signal(0);
  offsetY = signal(0);

  private dragging = false;
  private dragStartX = 0;
  private dragStartY = 0;
  private dragStartOffsetX = 0;
  private dragStartOffsetY = 0;

  baseScale = computed(() => {
    const w = this.naturalWidth();
    const h = this.naturalHeight();
    if (!w || !h) return 0;
    return Math.max(this.viewportSize / w, this.viewportSize / h);
  });

  effectiveScale = computed(() => this.baseScale() * this.zoom());
  displayWidth = computed(() => this.naturalWidth() * this.effectiveScale());
  displayHeight = computed(() => this.naturalHeight() * this.effectiveScale());

  maxOffsetX = computed(() => Math.max(0, (this.displayWidth() - this.viewportSize) / 2));
  maxOffsetY = computed(() => Math.max(0, (this.displayHeight() - this.viewportSize) / 2));

  imageLeft = computed(() => (this.viewportSize - this.displayWidth()) / 2 + this.offsetX());
  imageTop = computed(() => (this.viewportSize - this.displayHeight()) / 2 + this.offsetY());

  ngOnInit(): void {
    const url = URL.createObjectURL(this.file());
    this.imageUrl.set(url);
    this.destroyRef.onDestroy(() => URL.revokeObjectURL(url));
  }

  onImageLoad(): void {
    const img = this.imageRef()?.nativeElement;
    if (!img) return;
    this.naturalWidth.set(img.naturalWidth);
    this.naturalHeight.set(img.naturalHeight);
  }

  onZoomInput(value: string): void {
    this.zoom.set(Math.min(MAX_ZOOM, Math.max(MIN_ZOOM, Number(value))));
    this.offsetX.set(Math.min(this.maxOffsetX(), Math.max(-this.maxOffsetX(), this.offsetX())));
    this.offsetY.set(Math.min(this.maxOffsetY(), Math.max(-this.maxOffsetY(), this.offsetY())));
  }

  onPointerDown(event: PointerEvent): void {
    this.dragging = true;
    this.dragStartX = event.clientX;
    this.dragStartY = event.clientY;
    this.dragStartOffsetX = this.offsetX();
    this.dragStartOffsetY = this.offsetY();
    (event.target as HTMLElement).setPointerCapture(event.pointerId);
  }

  onPointerMove(event: PointerEvent): void {
    if (!this.dragging) return;

    const dx = event.clientX - this.dragStartX;
    const dy = event.clientY - this.dragStartY;
    const maxX = this.maxOffsetX();
    const maxY = this.maxOffsetY();

    this.offsetX.set(Math.min(maxX, Math.max(-maxX, this.dragStartOffsetX + dx)));
    this.offsetY.set(Math.min(maxY, Math.max(-maxY, this.dragStartOffsetY + dy)));
  }

  onPointerUp(): void {
    this.dragging = false;
  }

  cancel(): void {
    this.cancelled.emit();
  }

  async confirm(): Promise<void> {
    const img = this.imageRef()?.nativeElement;
    if (!img || !this.imageReady()) return;

    const scale = this.effectiveScale();
    const sourceSize = this.viewportSize / scale;
    const sourceX = -this.imageLeft() / scale;
    const sourceY = -this.imageTop() / scale;

    const canvas = document.createElement('canvas');
    canvas.width = OUTPUT_SIZE;
    canvas.height = OUTPUT_SIZE;
    const ctx = canvas.getContext('2d')!;
    ctx.drawImage(img, sourceX, sourceY, sourceSize, sourceSize, 0, 0, OUTPUT_SIZE, OUTPUT_SIZE);

    const blob = await new Promise<Blob | null>(resolve => canvas.toBlob(resolve, 'image/jpeg', 0.92));
    if (blob) this.cropped.emit(blob);
  }
}
