import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Capsule } from '../../interfaces/capsule';
import { CapsuleScroll } from './capsule-scroll';

const opened: Capsule = {
  id: 4,
  title: 'Pismo za 5 godina',
  message: 'Test123',
  openAt: '2026-09-19',
  openedAt: null,
  isUnlocked: true,
  canOpenNow: false,
  createdByUsername: 'test2',
  createdAt: '2026-09-17T10:00:00Z',
};

describe('CapsuleScroll', () => {
  let fixture: ComponentFixture<CapsuleScroll>;
  let component: CapsuleScroll;

  const el = () => fixture.nativeElement as HTMLElement;
  const head = () => el().querySelector('.head') as HTMLButtonElement;

  beforeEach(async () => {
    localStorage.setItem('lang', 'bs');
    await TestBed.configureTestingModule({ imports: [CapsuleScroll] }).compileComponents();

    fixture = TestBed.createComponent(CapsuleScroll);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('capsule', opened);
    fixture.detectChanges();
  });

  afterEach(() => localStorage.removeItem('lang'));

  it('offers both the unroll and the roll-up wording', () => {
    expect(el().querySelector('.cue-open')?.textContent).toContain('Razmotaj');
    expect(el().querySelector('.cue-close')?.textContent).toContain('Smotaj');
  });

  it('starts rolled up but keeps the title visible', () => {
    expect(head().getAttribute('aria-expanded')).toBe('false');
    expect(el().querySelector('.title')?.textContent).toContain('Pismo za 5 godina');
    expect(el().classList.contains('open')).toBe(false);
  });

  it('unrolls by itself when asked to start open', () => {
    fixture.componentRef.setInput('startOpen', true);
    fixture.detectChanges();

    expect(head().getAttribute('aria-expanded')).toBe('true');
    expect(el().classList.contains('open')).toBe(true);
  });

  it('unrolls and rolls up again when the header is clicked', () => {
    head().click();
    fixture.detectChanges();

    expect(head().getAttribute('aria-expanded')).toBe('true');
    expect(el().classList.contains('open')).toBe(true);
    head().click();
    fixture.detectChanges();

    expect(head().getAttribute('aria-expanded')).toBe('false');
    expect(el().classList.contains('open')).toBe(false);
  });

  it('points the header at the letter it controls', () => {
    const controlled = head().getAttribute('aria-controls');

    expect(controlled).toBe('letter-4');
    expect(el().querySelector(`#${controlled}`)?.textContent).toContain('Test123');
  });

  it('uses the date it opened, then the planned date, then when it was made', () => {
    expect(component.openedOn()).toBe('2026-09-19');

    fixture.componentRef.setInput('capsule', { ...opened, openedAt: '2026-09-20T08:00:00Z' });
    expect(component.openedOn()).toBe('2026-09-20T08:00:00Z');

    fixture.componentRef.setInput('capsule', { ...opened, openAt: null });
    expect(component.openedOn()).toBe('2026-09-17T10:00:00Z');
  });

  it('asks to delete only when allowed', () => {
    expect(el().querySelector('.delete')).toBeNull();

    fixture.componentRef.setInput('canDelete', true);
    fixture.detectChanges();
    const deleted = vi.fn();
    component.deleteRequested.subscribe(deleted);

    (el().querySelector('.delete') as HTMLButtonElement).click();

    expect(deleted).toHaveBeenCalledTimes(1);
  });
});
