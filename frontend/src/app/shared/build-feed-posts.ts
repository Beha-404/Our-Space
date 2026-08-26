import { AudioMessage } from '../interfaces/audio';
import { Photo } from '../interfaces/photo';

export interface FeedPost {
  id: number;
  type: 'photo' | 'audio';
  date: string;
  caption: string | null;
  imageUrl?: string;
  audioUrl?: string;
  uploadedByUsername: string;
}

export function buildFeedPosts(
  photos: Photo[],
  audioItems: AudioMessage[],
  resolvePhotoUrl: (path: string) => string,
  resolveAudioUrl: (path: string) => string,
  sortOrder: 'newest' | 'oldest' = 'newest',
): FeedPost[] {
  const photoPosts: FeedPost[] = photos.map(p => ({
    id: p.id,
    type: 'photo',
    date: p.takenAt,
    caption: p.caption,
    imageUrl: resolvePhotoUrl(p.thumbnailUrl),
    uploadedByUsername: p.uploadedByUsername,
  }));
  const audioPosts: FeedPost[] = audioItems.map(a => ({
    id: a.id,
    type: 'audio',
    date: a.recordedAt,
    caption: a.caption,
    audioUrl: resolveAudioUrl(a.url),
    uploadedByUsername: a.uploadedByUsername,
  }));

  const all = [...photoPosts, ...audioPosts].sort((a, b) => a.date.localeCompare(b.date));
  return sortOrder === 'newest' ? all.reverse() : all;
}
