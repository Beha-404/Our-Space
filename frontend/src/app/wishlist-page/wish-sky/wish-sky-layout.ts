export interface SkyPoint {
    x: number;
    y: number;
}

export const SKY_CAPACITY = 30;

const COLUMNS = 6;
const ROWS = 5;
const EDGE = 4;

const cellOrder = shuffledCells();

function shuffledCells(): number[] {
    const cells = Array.from({ length: COLUMNS * ROWS }, (_, index) => index);
    let seed = 7;

    for (let i = cells.length - 1; i > 0; i--) {
        seed = (seed * 9301 + 49297) % 233280;
        const j = seed % (i + 1);
        [cells[i], cells[j]] = [cells[j], cells[i]];
    }

    return cells;
}

function jitter(id: number, salt: number): number {
    const value = Math.sin(id * 12.9898 + salt * 78.233) * 43758.5453;
    return value - Math.floor(value);
}

function place(cell: number, cells: number, jitterValue: number): number {
    const spread = (cell + 0.5 + (jitterValue - 0.5) * 0.6) / cells;
    return EDGE + spread * (100 - EDGE * 2);
}

export function skyPoints(ids: number[]): Map<number, SkyPoint> {
    const points = new Map<number, SkyPoint>();
    const ordered = [...ids].sort((a, b) => a - b).slice(-SKY_CAPACITY);

    ordered.forEach((id, index) => {
        const cell = cellOrder[index];
        points.set(id, {
            x: place(cell % COLUMNS, COLUMNS, jitter(id, 1)),
            y: place(Math.floor(cell / COLUMNS), ROWS, jitter(id, 2)),
        });
    });

    return points;
}

export function skyDust(count: number): { x: number; y: number; opacity: number }[] {
    let seed = 41;
    const next = () => {
        seed = (seed * 9301 + 49297) % 233280;
        return seed / 233280;
    };

    return Array.from({ length: count }, () => ({
        x: next() * 100,
        y: next() * 100,
        opacity: 0.25 + next() * 0.55,
    }));
}
