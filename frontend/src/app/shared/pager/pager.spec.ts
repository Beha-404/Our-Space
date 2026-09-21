import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Pager } from './pager';

describe('Pager', () => {
  let fixture: ComponentFixture<Pager>;
  let component: Pager;

  const el = () => fixture.nativeElement as HTMLElement;

  function render(page: number, totalPages: number) {
    fixture.componentRef.setInput('page', page);
    fixture.componentRef.setInput('totalPages', totalPages);
    fixture.detectChanges();
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [Pager] }).compileComponents();
    fixture = TestBed.createComponent(Pager);
    component = fixture.componentInstance;
  });

  it('marks the current page and shortens long ranges with dots', () => {
    render(5, 20);

    const labels = [...el().querySelectorAll('.num, .ellipsis')].map(n => n.textContent?.trim());
    expect(labels).toEqual(['1', '…', '4', '5', '6', '…', '20']);
    expect(el().querySelector('.num.active')?.textContent?.trim()).toBe('5');
    expect(el().querySelector('[aria-current="page"]')).not.toBeNull();
  });

  it('asks for the page that was clicked', () => {
    render(1, 3);
    const changed = vi.fn();
    component.pageChange.subscribe(changed);

    (el().querySelectorAll('.num')[2] as HTMLButtonElement).click();

    expect(changed).toHaveBeenCalledWith(3);
  });

  it('disables the arrows at both ends and never goes out of range', () => {
    render(1, 3);
    const changed = vi.fn();
    component.pageChange.subscribe(changed);

    const [previous, next] = el().querySelectorAll<HTMLButtonElement>('.btn');
    expect(previous.disabled).toBe(true);
    expect(next.disabled).toBe(false);

    component.go(0);
    component.go(1);
    expect(changed).not.toHaveBeenCalled();

    component.go(99);
    expect(changed).toHaveBeenCalledWith(3);
  });
});
