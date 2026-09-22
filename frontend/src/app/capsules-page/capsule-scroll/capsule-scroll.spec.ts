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

  const el = () => fixture.nativeElement as HTMLElement;

  beforeEach(async () => {
    localStorage.setItem('lang', 'bs');
    await TestBed.configureTestingModule({ imports: [CapsuleScroll] }).compileComponents();

    fixture = TestBed.createComponent(CapsuleScroll);
    fixture.componentRef.setInput('capsule', opened);
    fixture.detectChanges();
  });

  afterEach(() => localStorage.removeItem('lang'));

  it('always shows the letter unrolled, title and message both visible', () => {
    expect(el().querySelector('.title')?.textContent).toContain('Pismo za 5 godina');
    expect(el().querySelector('.letter')?.textContent).toContain('Test123');
  });

  it('puts the opened date at the bottom of the card, not the top', () => {
    const title = el().querySelector('.title') as HTMLElement;
    const meta = el().querySelector('.meta') as HTMLElement;

    expect(meta.textContent).toContain('Otvorena');
    expect(title.compareDocumentPosition(meta) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
  });

  it('asks to delete only when allowed', () => {
    expect(el().querySelector('.delete')).toBeNull();

    fixture.componentRef.setInput('canDelete', true);
    fixture.detectChanges();
    const deleted = vi.fn();
    fixture.componentInstance.deleteRequested.subscribe(deleted);

    (el().querySelector('.delete') as HTMLButtonElement).click();

    expect(deleted).toHaveBeenCalledTimes(1);
  });
});
