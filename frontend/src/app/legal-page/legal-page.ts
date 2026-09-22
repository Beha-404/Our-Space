import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslatePipe } from '../i18n/translate.pipe';

type LegalSection = { titleKey: string; bodyKey: string };

@Component({
  imports: [RouterLink, TranslatePipe],
  selector: 'app-legal-page',
  styleUrl: './legal-page.css',
  templateUrl: './legal-page.html',
})
export class LegalPage {
  private route = inject(ActivatedRoute);

  topic = (this.route.snapshot.data['topic'] as 'terms' | 'privacy') ?? 'terms';

  titleKey = this.topic === 'terms' ? 'legal.termsTitle' : 'legal.privacyTitle';
  introKey = this.topic === 'terms' ? 'legal.termsIntro' : 'legal.privacyIntro';
  contactTitleKey = this.topic === 'terms' ? 'legal.termsContactTitle' : 'legal.privacyContactTitle';
  contactBodyKey = this.topic === 'terms' ? 'legal.termsContactBody' : 'legal.privacyContactBody';

  sections: LegalSection[] = this.topic === 'terms'
    ? [
        { titleKey: 'legal.termsAccountTitle', bodyKey: 'legal.termsAccountBody' },
        { titleKey: 'legal.termsContentTitle', bodyKey: 'legal.termsContentBody' },
        { titleKey: 'legal.termsBehaviorTitle', bodyKey: 'legal.termsBehaviorBody' },
        { titleKey: 'legal.termsAvailabilityTitle', bodyKey: 'legal.termsAvailabilityBody' },
        { titleKey: 'legal.termsChangesTitle', bodyKey: 'legal.termsChangesBody' },
      ]
    : [
        { titleKey: 'legal.privacyDataTitle', bodyKey: 'legal.privacyDataBody' },
        { titleKey: 'legal.privacyUseTitle', bodyKey: 'legal.privacyUseBody' },
        { titleKey: 'legal.privacyStorageTitle', bodyKey: 'legal.privacyStorageBody' },
        { titleKey: 'legal.privacyThirdPartyTitle', bodyKey: 'legal.privacyThirdPartyBody' },
        { titleKey: 'legal.privacyCookiesTitle', bodyKey: 'legal.privacyCookiesBody' },
        { titleKey: 'legal.privacySecurityTitle', bodyKey: 'legal.privacySecurityBody' },
        { titleKey: 'legal.privacyRightsTitle', bodyKey: 'legal.privacyRightsBody' },
      ];
}
