import { AudioMessage } from '../interfaces/audio';
import { Photo } from '../interfaces/photo';

export interface FeedPost {
  id: number;
  type: 'photo' | 'audio';
  date: string;
  caption: string | null;
  imageUrl?: string;
  thumbnailUrl?: string;
  mediumUrl?: string;
  audioUrl?: string;
  downloadUrl: string;
  uploadedByUsername: string;
  status?: 'ready' | 'processing' | 'failed';
}

function withDownload(url: string, caption: string | null, date: string): string {
  const name = caption?.trim() || `ourspace-${date.slice(0, 10)}`;
  const separator = url.includes('?') ? '&' : '?';

  return `${url}${separator}download=1&name=${encodeURIComponent(name)}`;
}

export function toFeedPost(
  item: {
    id: number;
    type: 'photo' | 'audio';
    url: string;
    thumbnailUrl: string | null;
    mediumUrl: string | null;
    caption: string | null;
    status: 'ready' | 'processing' | 'failed';
    date: string;
    uploadedByUsername: string;
  },
  resolveUrl: (path: string) => string,
): FeedPost {
  const url = resolveUrl(item.url);

  return {
    id: item.id,
    type: item.type,
    date: item.date,
    caption: item.caption,
    imageUrl: item.type === 'photo' ? url : undefined,
    thumbnailUrl: item.thumbnailUrl ? resolveUrl(item.thumbnailUrl) : undefined,
    mediumUrl: item.mediumUrl ? resolveUrl(item.mediumUrl) : undefined,
    audioUrl: item.type === 'audio' ? url : undefined,
    downloadUrl: withDownload(url, item.caption, item.date),
    uploadedByUsername: item.uploadedByUsername,
    status: item.status,
  };
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
      mediumUrl: p.mediumUrl ? resolvePhotoUrl(p.mediumUrl) : undefined,
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
