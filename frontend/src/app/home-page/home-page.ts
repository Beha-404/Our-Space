import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { TranslatePipe } from '../i18n/translate.pipe';
import { Navbar } from '../navbar/navbar';
import { EventItem } from '../interfaces/event';
import { EventService } from '../services/event.service';
import { PhotoService } from '../services/photo.service';
import { AudioService } from '../services/audio.service';
import { UserService } from '../services/user.service';
import { buildTimelineItems } from '../shared/build-timeline-items';
import { TimelineGraph } from '../memories-page/timeline-graph/timeline-graph';
import { Photo } from '../interfaces/photo';
import { AudioMessage } from '../interfaces/audio';
import { Avatar } from '../shared/avatar/avatar';

@Component({
  imports: [Navbar, RouterLink, TranslatePipe, DatePipe, TimelineGraph, Avatar],
  selector: 'app-home-page',
  styleUrl: './home-page.css',
  templateUrl: './home-page.html',
})
export class HomePage {
  userService = inject(UserService);
  private eventService = inject(EventService);
  private photoService = inject(PhotoService);
  private audioService = inject(AudioService);

  upcomingEvents = signal<EventItem[]>([]);
  photos = signal<Photo[]>([]);
  audioItems = signal<AudioMessage[]>([]);

  timelineItems = computed(() =>
    buildTimelineItems(this.photos(), this.audioItems(), path => this.photoService.fullUrl(path))
  );

  daysTogether = computed(() => {
    const partner = this.userService.currentUser()?.partner;
    if (!partner?.relationshipStartDate) return null;

    const start = new Date(partner.relationshipStartDate).getTime();
    const diffMs = Date.now() - start;
    return Math.max(0, Math.floor(diffMs / (1000 * 60 * 60 * 24)));
  });

  constructor() {
    this.userService.refreshCurrentUser().subscribe(user => {
      if (user.partner) {
        this.eventService.getUpcoming().subscribe(events => this.upcomingEvents.set(events.slice(0, 3)));
        forkJoin({
          photos: this.photoService.getAll(),
          audio: this.audioService.getAll(),
        }).subscribe(({ photos, audio }) => {
          this.photos.set(photos);
          this.audioItems.set(audio);
        });
      }
    });
  }
}
