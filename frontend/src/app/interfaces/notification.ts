export type NotificationType = 'EventCreated' | 'EventUpdated' | 'EventDeleted' | 'PhotoAdded' | 'AudioAdded' | 'WishAdded';

export interface AppNotification {
    id: number;
    type: NotificationType;
    entityType: 'event' | 'photo' | 'audio' | 'wish';
    entityId: number;
    entityTitle: string;
    actorUsername: string;
    createdAt: string;
    isRead: boolean;
}
