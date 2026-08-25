import { AudioMessage } from '../interfaces/audio';
import { Photo } from '../interfaces/photo';
import { TimelineItem } from '../memories-page/timeline-graph/timeline-graph';

export function buildTimelineItems(
  photos: Photo[],
  audioItems: AudioMessage[],
  resolveThumbUrl: (path: string) => string,
): TimelineItem[] {
  const photoItems: TimelineItem[] = photos.map(p => ({
    id: p.id,
    type: 'photo',
    date: p.takenAt,
    caption: p.caption,
    thumbnailUrl: resolveThumbUrl(p.thumbnailUrl),
  }));

  const audio: TimelineItem[] = audioItems.map(a => ({
    id: a.id,
    type: 'audio',
    date: a.recordedAt,
    caption: a.caption,
  }));

  return [...photoItems, ...audio].sort((a, b) => a.date.localeCompare(b.date));
}
