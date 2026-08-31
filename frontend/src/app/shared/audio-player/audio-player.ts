import { Component, computed, ElementRef, input, signal, viewChild } from '@angular/core';
import { TranslatePipe } from '../../i18n/translate.pipe';

@Component({
  selector: 'app-audio-player',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './audio-player.html',
  styleUrl: './audio-player.css',
})
export class AudioPlayer {
  src = input.required<string>();

  private audioEl = viewChild<ElementRef<HTMLAudioElement>>('audioEl');
  private trackEl = viewChild<ElementRef<HTMLDivElement>>('trackEl');

  playing = signal(false);
  currentTime = signal(0);
  duration = signal(0);

  progressPercent = computed(() => {
    const total = this.duration();
    return total > 0 ? (this.currentTime() / total) * 100 : 0;
  });

  timeLabel = computed(() => {
    const showElapsed = this.playing() || this.currentTime() > 0;
    return this.format(showElapsed ? this.currentTime() : this.duration());
  });

  togglePlay(): void {
    const audio = this.audioEl()?.nativeElement;
    if (!audio) return;

    if (audio.paused) {
      audio.play();
      this.playing.set(true);
    } else {
      audio.pause();
      this.playing.set(false);
    }
  }

  onTimeUpdate(): void {
    const audio = this.audioEl()?.nativeElement;
    if (audio) this.currentTime.set(audio.currentTime);
  }

  onLoadedMetadata(): void {
    const audio = this.audioEl()?.nativeElement;
    if (audio) this.duration.set(audio.duration);
  }

  onEnded(): void {
    this.playing.set(false);
    this.currentTime.set(0);
  }

  seek(event: MouseEvent): void {
    const audio = this.audioEl()?.nativeElement;
    const track = this.trackEl()?.nativeElement;
    if (!audio || !track || !this.duration()) return;

    const rect = track.getBoundingClientRect();
    const ratio = Math.min(Math.max((event.clientX - rect.left) / rect.width, 0), 1);
    audio.currentTime = ratio * this.duration();
    this.currentTime.set(audio.currentTime);
  }

  private format(seconds: number): string {
    if (!isFinite(seconds) || seconds < 0) return '0:00';
    const m = Math.floor(seconds / 60);
    const s = Math.floor(seconds % 60);
    return `${m}:${s.toString().padStart(2, '0')}`;
  }
}
