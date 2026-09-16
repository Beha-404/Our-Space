export type NotificationType =
    | 'EventCreated'
    | 'EventUpdated'
    | 'EventDeleted'
    | 'PhotoAdded'
    | 'AudioAdded'
    | 'WishAdded'
    | 'CapsuleSealed'
    | 'CapsuleUnlocked';

export interface AppNotification {
    id: number;
    type: NotificationType;
    entityType: 'event' | 'photo' | 'audio' | 'wish' | 'capsule';
    entityId: number;
    entityTitle: string;
    actorUsername: string;
    createdAt: string;
    isRead: boolean;
}
