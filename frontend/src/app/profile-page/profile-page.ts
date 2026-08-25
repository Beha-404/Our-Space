import { DatePipe } from '@angular/common';
import { Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '../i18n/translate.pipe';
import { Navbar } from '../navbar/navbar';
import { Avatar } from '../shared/avatar/avatar';
import { AuthService } from '../services/auth.service';
import { UserService } from '../services/user.service';

@Component({
  imports: [Navbar, DatePipe, TranslatePipe, Avatar],
  selector: 'app-profile-page',
  styleUrl: './profile-page.css',
  templateUrl: './profile-page.html',
})
export class ProfilePage {
  private userService = inject(UserService);
  private authService = inject(AuthService);
  private router = inject(Router);

  currentUser = this.userService.currentUser;

  pictureFileInput = viewChild<ElementRef<HTMLInputElement>>('pictureFileInput');
  uploadingPicture = signal(false);
  pictureErrorKey = signal('');

  formData = signal({ displayName: '' });
  saving = signal(false);
  saveMessageKey = signal('');
  saveErrorKey = signal('');

  pairingCode = signal<{ code: string; expiresAt: string } | null>(null);
  pairingLoading = signal(false);
  pairInput = signal('');
  relationshipDateInput = signal('');
  pairErrorKey = signal('');

  deleteConfirming = signal(false);
  deleteErrorKey = signal('');

  codeCopied = signal(false);

  editingRelationshipDate = signal(false);
  relationshipDateEdit = signal('');
  relationshipDateSaving = signal(false);
  relationshipDateErrorKey = signal('');

  constructor() {
    this.userService.refreshCurrentUser().subscribe(user => {
      this.formData.set({
        displayName: user.displayName ?? '',
      });
    });
  }

  updateField(field: 'displayName', value: string): void {
    this.formData.update(data => ({ ...data, [field]: value }));
  }

  saveProfile(): void {
    this.saving.set(true);
    this.saveMessageKey.set('');
    this.saveErrorKey.set('');

    const data = this.formData();
    this.userService.updateUser({
      displayName: data.displayName || null,
      profilePictureUrl: null,
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.saveMessageKey.set('profile.saved');
      },
      error: () => {
        this.saving.set(false);
        this.saveErrorKey.set('profile.saveError');
      }
    });
  }

  onPictureSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.uploadingPicture.set(true);
    this.pictureErrorKey.set('');

    this.userService.uploadProfilePicture(file).subscribe({
      next: () => {
        this.uploadingPicture.set(false);
        const fileInput = this.pictureFileInput()?.nativeElement;
        if (fileInput) fileInput.value = '';
      },
      error: () => {
        this.uploadingPicture.set(false);
        this.pictureErrorKey.set('profile.pictureUploadError');
      }
    });
  }

  generateCode(): void {
    this.pairingLoading.set(true);
    this.pairErrorKey.set('');

    this.userService.generatePairingCode().subscribe({
      next: (res) => {
        this.pairingLoading.set(false);
        this.pairingCode.set(res);
      },
      error: () => {
        this.pairingLoading.set(false);
        this.pairErrorKey.set('profile.pairingCodeError');
      }
    });
  }

  copyCode(): void {
    const code = this.pairingCode()?.code;
    if (!code) return;

    navigator.clipboard.writeText(code);
    this.codeCopied.set(true);
    setTimeout(() => this.codeCopied.set(false), 2000);
  }

  submitPair(): void {
    this.pairErrorKey.set('');
    const code = this.pairInput().trim();

    if (!code) {
      this.pairErrorKey.set('profile.pairErrorEmpty');
      return;
    }

    this.userService.pair({
      code,
      relationshipStartDate: this.relationshipDateInput() || null,
    }).subscribe({
      next: () => {
        this.pairingCode.set(null);
        this.pairInput.set('');
      },
      error: () => this.pairErrorKey.set('profile.pairError')
    });
  }

  startEditRelationshipDate(): void {
    this.relationshipDateErrorKey.set('');
    this.relationshipDateEdit.set(this.currentUser()?.partner?.relationshipStartDate ?? '');
    this.editingRelationshipDate.set(true);
  }

  cancelEditRelationshipDate(): void {
    this.editingRelationshipDate.set(false);
  }

  saveRelationshipDate(): void {
    const date = this.relationshipDateEdit();
    if (!date) {
      this.relationshipDateErrorKey.set('profile.dateEmptyError');
      return;
    }

    this.relationshipDateSaving.set(true);
    this.relationshipDateErrorKey.set('');

    this.userService.setRelationshipDate(date).subscribe({
      next: () => {
        this.relationshipDateSaving.set(false);
        this.editingRelationshipDate.set(false);
      },
      error: () => {
        this.relationshipDateSaving.set(false);
        this.relationshipDateErrorKey.set('profile.dateError');
      }
    });
  }

  confirmDelete(): void {
    this.deleteConfirming.set(true);
  }

  cancelDelete(): void {
    this.deleteConfirming.set(false);
  }

  deleteAccount(): void {
    const user = this.currentUser();
    if (!user) return;

    this.deleteErrorKey.set('');
    this.userService.deleteUser(user.id).subscribe({
      next: () => {
        this.authService.logout();
        this.router.navigate(['/login']);
      },
      error: () => this.deleteErrorKey.set('profile.deleteError')
    });
  }
}
