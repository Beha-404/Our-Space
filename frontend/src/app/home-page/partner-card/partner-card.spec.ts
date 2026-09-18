import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { config } from '../../config';
import { PartnerActivity } from '../../services/home.service';
import { PartnerCard } from './partner-card';

describe('PartnerCard', () => {
  let component: PartnerCard;
  let fixture: ComponentFixture<PartnerCard>;
  let httpMock: HttpTestingController;

  const activity: PartnerActivity = {
    photos: 5,
    voiceLetters: 2,
    wishes: 1,
    events: 3,
    capsules: 0,
    recentPhotos: [{ id: 7, thumbnailUrl: '/uploads/t.jpg?sig=1', caption: 'Kafa', takenAt: '2026-09-15' }],
  };

  const partnerUrl = `${config.apiUrl}/home/partner`;

  beforeEach(async () => {
    localStorage.setItem('lang', 'bs');

    await TestBed.configureTestingModule({
      imports: [PartnerCard],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(PartnerCard);
    fixture.componentRef.setInput('partner', { id: 2, username: 'test2', profilePictureUrl: null, relationshipStartDate: null });
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.removeItem('lang');
  });

  it('loads what the partner added only when the card is opened', () => {
    httpMock.expectNone(partnerUrl);

    component.togglePartner();
    httpMock.expectOne(partnerUrl).flush(activity);

    expect(component.partnerOpen()).toBe(true);
    expect(component.partnerActivity()).toEqual(activity);
  });

  it('does not ask again when the card is closed and opened', () => {
    component.togglePartner();
    httpMock.expectOne(partnerUrl).flush(activity);

    component.togglePartner();
    component.togglePartner();

    httpMock.expectNone(partnerUrl);
    expect(component.partnerOpen()).toBe(true);
  });

  it('uses the right word form for each count and links to the matching page', () => {
    component.togglePartner();
    httpMock.expectOne(partnerUrl).flush(activity);

    expect(component.partnerStats()).toEqual([
      { count: 5, labelKey: 'partner.photosMany', link: '/memories', queryParams: { filter: 'photo' } },
      { count: 2, labelKey: 'partner.lettersFew', link: '/memories', queryParams: { filter: 'audio' } },
      { count: 1, labelKey: 'partner.wishesOne', link: '/wishlist', queryParams: null },
      { count: 3, labelKey: 'partner.eventsFew', link: '/events', queryParams: null },
      { count: 0, labelKey: 'partner.capsulesMany', link: '/capsules', queryParams: null },
    ]);
  });

  it('draws the counts and the photos once they arrive', () => {
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('.partner-chip:not(.is-loading)').length).toBe(0);

    component.togglePartner();
    httpMock.expectOne(partnerUrl).flush(activity);
    fixture.detectChanges();

    const chips: HTMLElement[] = [...fixture.nativeElement.querySelectorAll('.partner-chip:not(.is-loading)')];
    expect(chips.map(c => c.textContent?.replace(/\s+/g, ' ').trim())).toEqual([
      '5 slika', '2 glasovna pisma', '1 želja', '3 događaja', '0 kapsula',
    ]);
    expect(fixture.nativeElement.querySelectorAll('.partner-photo').length).toBe(1);
    expect(fixture.nativeElement.querySelector('.partner-photo-caption').textContent).toContain('Kafa');
  });

  it('lets the user try again after a failed load', () => {
    component.togglePartner();
    httpMock.expectOne(partnerUrl).flush(null, { status: 500, statusText: 'Server Error' });
    expect(component.partnerActivityFailed()).toBe(true);

    component.loadPartnerActivity();
    expect(component.partnerActivityFailed()).toBe(false);
    httpMock.expectOne(partnerUrl).flush(activity);

    expect(component.partnerActivity()).toEqual(activity);
  });
});
