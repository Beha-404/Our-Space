import { TestBed } from '@angular/core/testing';
import { NavigationEnd, provideRouter, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { BOOT_NOTICE_DELAY_MS, BOOT_SLOW_DELAY_MS, BootScreen } from './boot-screen';

describe('BootScreen', () => {
  let events: Subject<unknown>;

  beforeEach(() => {
    vi.useFakeTimers();
    events = new Subject();
    TestBed.configureTestingModule({
      imports: [BootScreen],
      providers: [provideRouter([])],
    });
    const router = TestBed.inject(Router);
    Object.defineProperty(router, 'events', { value: events });
  });

  afterEach(() => vi.useRealTimers());

  function render() {
    const fixture = TestBed.createComponent(BootScreen);
    fixture.detectChanges();
    return fixture;
  }

  function notice(fixture: ReturnType<typeof render>) {
    fixture.detectChanges();
    return (fixture.nativeElement as HTMLElement).querySelector('.boot-screen');
  }

  it('stays hidden when the first page loads quickly', () => {
    const fixture = render();
    events.next(new NavigationEnd(1, '/', '/'));
    vi.advanceTimersByTime(BOOT_NOTICE_DELAY_MS + 10);
    expect(notice(fixture)).toBeNull();
  });

  it('shows the notice while the server is still waking up', () => {
    const fixture = render();
    vi.advanceTimersByTime(BOOT_NOTICE_DELAY_MS + 10);
    expect(notice(fixture)).not.toBeNull();
    expect(fixture.nativeElement.querySelector('button')).toBeNull();
  });

  it('offers a refresh when waking up takes too long', () => {
    const fixture = render();
    vi.advanceTimersByTime(BOOT_SLOW_DELAY_MS + 10);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('button')).not.toBeNull();
  });

  it('disappears once the first navigation finishes', () => {
    const fixture = render();
    vi.advanceTimersByTime(BOOT_NOTICE_DELAY_MS + 10);
    expect(notice(fixture)).not.toBeNull();
    events.next(new NavigationEnd(1, '/home', '/home'));
    fixture.detectChanges();
    expect(notice(fixture)).toBeNull();
  });
});
