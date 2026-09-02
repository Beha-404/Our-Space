import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { LanguageSwitcher } from '../i18n/language-switcher/language-switcher';
import { TranslatePipe } from '../i18n/translate.pipe';
import { Avatar } from '../shared/avatar/avatar';
import { UserService } from '../services/user.service';

@Component({
  imports: [RouterLink, RouterLinkActive, TranslatePipe, LanguageSwitcher, Avatar],
  selector: 'app-navbar',
  styleUrl: './navbar.css',
  templateUrl: './navbar.html',
})
export class Navbar {
  userService = inject(UserService);

  constructor() {
    this.userService.refreshCurrentUser().subscribe();
  }
}
