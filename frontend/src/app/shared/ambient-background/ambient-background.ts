import { Component } from '@angular/core';

interface Mote {
  left: number;
  size: number;
  duration: number;
  delay: number;
  drift: number;
  glow: number;
}

const MOTE_COUNT = 22;

function seeded(seed: number): () => number {
  let state = seed;
  return () => {
    state = (state * 16807) % 2147483647;
    return (state - 1) / 2147483646;
  };
}

function createMotes(): Mote[] {
  const random = seeded(20260917);

  return Array.from({ length: MOTE_COUNT }, () => {
    const duration = 22 + random() * 26;
    return {
      left: random() * 100,
      size: 2 + random() * 3,
      duration,
      delay: -random() * duration,
      drift: (random() - 0.5) * 120,
      glow: 0.35 + random() * 0.45,
    };
  });
}

@Component({
  selector: 'app-ambient-background',
  templateUrl: './ambient-background.html',
  styleUrl: './ambient-background.css',
  host: { 'aria-hidden': 'true' },
})
export class AmbientBackground {
  readonly motes = createMotes();
}
