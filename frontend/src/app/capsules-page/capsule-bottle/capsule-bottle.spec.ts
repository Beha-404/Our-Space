import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Capsule } from '../../interfaces/capsule';
import { CapsuleBottle, UNCORK_MS } from './capsule-bottle';

function capsule(overrides: Partial<Capsule> = {}): Capsule {
  return {
    id: 1,
    title: 'Za našu petu godišnjicu',
    message: null,
    openAt: '2099-09-17',
    openedAt: null,
    isUnlocked: false,
    canOpenNow: false,
    createdByUsername: 'test2',
    createdAt: '2026-09-17T10:00:00Z',
    ...overrides,
  };
}

describe('CapsuleBottle', () => {
  let fixture: ComponentFixture<CapsuleBottle>;
  let component: CapsuleBottle;

  function render(cap: Capsule, extra: Record<string, unknown> = {}) {
    fixture.componentRef.setInput('capsule', cap);
    for (const [key, value] of Object.entries(extra)) fixture.componentRef.setInput(key, value);
    fixture.detectChanges();
  }

  const el = () => fixture.nativeElement as HTMLElement;

  beforeEach(async () => {
    localStorage.setItem('lang', 'bs');
    vi.useFakeTimers();
    await TestBed.configureTestingModule({ imports: [CapsuleBottle] }).compileComponents();
    fixture = TestBed.createComponent(CapsuleBottle);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    vi.useRealTimers();
    localStorage.removeItem('lang');
  });

  it('shows the title and how many days are left for a dated capsule', () => {
    render(capsule());

    expect(el().querySelector('h3')?.textContent).toContain('Za našu petu godišnjicu');
    expect(el().querySelector('.pill')?.textContent).toMatch(/\d+/);
    expect(el().querySelector('.btn-primary')).toBeNull();
    expect(el().classList.contains('available')).toBe(false);
  });

  it('never shows a negative number of days', () => {
    render(capsule({ openAt: '2000-01-01' }));

    expect(component.daysLeft()).toBe(0);
  });

  it('glows and offers to open a capsule that has no date', () => {
    render(capsule({ openAt: null, canOpenNow: true }));

    expect(el().classList.contains('available')).toBe(true);
    expect(el().querySelector('.pill.ready')).not.toBeNull();
    expect(el().querySelector('.btn-primary')).not.toBeNull();
    expect(el().querySelectorAll('.spark').length).toBe(3);
  });

  it('pops the cork first and asks to open only after the animation', () => {
    render(capsule({ openAt: null, canOpenNow: true }));
    const opened = vi.fn();
    component.openRequested.subscribe(opened);

    component.uncork();
    fixture.detectChanges();

    expect(el().querySelector('.bottle')?.classList.contains('uncorking')).toBe(true);
    expect(opened).not.toHaveBeenCalled();

    vi.advanceTimersByTime(UNCORK_MS);
    fixture.detectChanges();

    expect(opened).toHaveBeenCalledTimes(1);
    expect(component.uncorking()).toBe(false);
  });

  it('ignores a second click while the cork is popping', () => {
    render(capsule({ openAt: null, canOpenNow: true }));
    const opened = vi.fn();
    component.openRequested.subscribe(opened);

    component.uncork();
    component.uncork();
    vi.advanceTimersByTime(UNCORK_MS);

    expect(opened).toHaveBeenCalledTimes(1);
  });

  it('does not open again while a request is already running', () => {
    render(capsule({ openAt: null, canOpenNow: true }), { opening: true });
    const opened = vi.fn();
    component.openRequested.subscribe(opened);

    component.uncork();
    vi.advanceTimersByTime(UNCORK_MS);

    expect(opened).not.toHaveBeenCalled();
  });

  it('asks to delete only when deleting is allowed', () => {
    render(capsule());
    expect(el().querySelector('.tag-delete')).toBeNull();

    fixture.componentRef.setInput('canDelete', true);
    fixture.detectChanges();
    const deleted = vi.fn();
    component.deleteRequested.subscribe(deleted);

    (el().querySelector('.tag-delete') as HTMLButtonElement).click();

    expect(deleted).toHaveBeenCalledTimes(1);
  });

  it('gives every capsule its own tilt and bobbing rhythm', () => {
    render(capsule({ id: 1 }));
    const first = [component.tilt(), component.bobDuration(), component.bobDelay()];

    fixture.componentRef.setInput('capsule', capsule({ id: 2 }));
    fixture.detectChanges();
    const second = [component.tilt(), component.bobDuration(), component.bobDelay()];

    expect(second).not.toEqual(first);
  });

  it('pauses its animations while it is scrolled out of view', () => {
    let notify: (entries: { isIntersecting: boolean }[]) => void = () => undefined;
    vi.stubGlobal('IntersectionObserver', class {
      constructor(callback: typeof notify) {
        notify = callback;
      }
      observe() {}
      disconnect() {}
    });

    render(capsule());
    expect(el().classList.contains('paused')).toBe(false);

    notify([{ isIntersecting: false }]);
    fixture.detectChanges();
    expect(el().classList.contains('paused')).toBe(true);

    notify([{ isIntersecting: true }]);
    fixture.detectChanges();
    expect(el().classList.contains('paused')).toBe(false);

    vi.unstubAllGlobals();
  });

  it('marks a freshly created bottle so it can splash in', () => {
    render(capsule(), { arriving: true });

    expect(el().classList.contains('arriving')).toBe(true);
    expect(el().querySelector('.ripple')).not.toBeNull();
  });
});
