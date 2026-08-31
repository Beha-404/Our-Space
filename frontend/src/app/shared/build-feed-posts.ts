import { AudioMessage } from '../interfaces/audio';
import { Photo } from '../interfaces/photo';

export interface FeedPost {
  id: number;
  type: 'photo' | 'audio';
  date: string;
  caption: string | null;
  imageUrl?: string;
  thumbnailUrl?: string;
  audioUrl?: string;
  downloadUrl: string;
  uploadedByUsername: string;
}

function withDownload(url: string, caption: string | null, date: string): string {
  const name = caption?.trim() || `ourspace-${date.slice(0, 10)}`;
  const separator = url.includes('?') ? '&' : '?';

  return `${url}${separator}download=1&name=${encodeURIComponent(name)}`;
}

export function buildFeedPosts(
  photos: Photo[],
  audioItems: AudioMessage[],
  resolvePhotoUrl: (path: string) => string,
  resolveAudioUrl: (path: string) => string,
  sortOrder: 'newest' | 'oldest' = 'newest',
): FeedPost[] {
  const photoPosts: FeedPost[] = photos.map(p => {
    const url = resolvePhotoUrl(p.url);

    return {
      id: p.id,
      type: 'photo',
      date: p.takenAt,
      caption: p.caption,
      imageUrl: url,
      thumbnailUrl: resolvePhotoUrl(p.thumbnailUrl),
      downloadUrl: withDownload(url, p.caption, p.takenAt),
      uploadedByUsername: p.uploadedByUsername,
    };
  });

  const audioPosts: FeedPost[] = audioItems.map(a => {
    const url = resolveAudioUrl(a.url);

    return {
      id: a.id,
      type: 'audio',
      date: a.recordedAt,
      caption: a.caption,
      audioUrl: url,
      downloadUrl: withDownload(url, a.caption, a.recordedAt),
      uploadedByUsername: a.uploadedByUsername,
    };
  });

  const all = [...photoPosts, ...audioPosts].sort((a, b) => a.date.localeCompare(b.date));
  return sortOrder === 'newest' ? all.reverse() : all;
}
