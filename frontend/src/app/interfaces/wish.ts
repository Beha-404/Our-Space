export interface Wish {
    id: number;
    title: string;
    isFulfilled: boolean;
    fulfilledAt: string | null;
    createdByUsername: string;
    createdAt: string;
}
