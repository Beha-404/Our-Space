export interface EventItem {
    id: number;
    title: string;
    description: string | null;
    eventDate: string;
    createdByUsername: string;
    createdAt: string;
}

export interface CreateEventRequest {
    title: string;
    description: string | null;
    eventDate: string;
}
