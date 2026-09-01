import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, ElementRef, inject, signal, viewChild } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '../i18n/translate.pipe';
import { Navbar } from '../navbar/navbar';
import { Avatar } from '../shared/avatar/avatar';
import { DatePicker } from '../shared/date-picker/date-picker';
import { AuthService } from '../services/auth.service';
import { UserService } from '../services/user.service';
import { ToastService } from '../shared/toast/toast.service';
import { isValidEmail } from '../shared/validators';

@Component({
  imports: [Navbar, DatePipe, TranslatePipe, Avatar, DatePicker],
  selector: 'app-profile-page',
  styleUrl: './profile-page.css',
  templateUrl: './profile-page.html',
})
export class ProfilePage {
  private userService = inject(UserService);
  private authService = inject(AuthService);
  private router = inject(Router);
  private toast = inject(ToastService);

  currentUser = this.userService.currentUser;

  pictureFileInput = viewChild<ElementRef<HTMLInputElement>>('pictureFileInput');
  uploadingPicture = signal(false);
  pictureErrorKey = signal('');

  formData = signal({ username: '' });
  saving = signal(false);
  saveMessageKey = signal('');
  saveErrorKey = signal('');
  usernameTouched = signal(false);

  usernameError = computed(() =>
    this.usernameTouched() && !this.formData().username.trim() ? 'profile.errUsernameRequired' : '');

  editingEmail = signal(false);
  newEmail = signal('');
  emailSending = signal(false);
  emailMessageKey = signal('');
  emailErrorKey = signal('');
  newEmailTouched = signal(false);

  newEmailError = computed(() => {
    if (!this.newEmailTouched()) return '';
    const value = this.newEmail().trim();
    if (!value) return 'auth.errEmailRequired';
    return isValidEmail(value) ? '' : 'auth.errInvalidEmail';
  });

  confirmCode = signal('');
  confirmingEmail = signal(false);
  confirmErrorKey = signal('');
  confirmCodeTouched = signal(false);

  confirmCodeError = computed(() =>
    this.confirmCodeTouched() && !this.confirmCode().trim() ? 'profile.emailConfirmError' : '');

  pairingCode = signal<{ code: string; expiresAt: string } | null>(null);
  pairingLoading = signal(false);
  pairInput = signal('');
  relationshipDateInput = signal('');
  pairErrorKey = signal('');
  pairInputTouched = signal(false);

  pairInputError = computed(() =>
    this.pairInputTouched() && !this.pairInput().trim() ? 'profile.pairErrorEmpty' : '');

  deleteConfirming = signal(false);
  deleteErrorKey = signal('');

  unpairConfirming = signal(false);
  unpairing = signal(false);
  unpairErrorKey = signal('');

  codeCopied = signal(false);

  editingRelationshipDate = signal(false);
  relationshipDateEdit = signal('');
  relationshipDateSaving = signal(false);
  relationshipDateErrorKey = signal('');

  constructor() {
    this.userService.refreshCurrentUser().subscribe(user => {
      this.formData.set({ username: user.username });
    });
  }

  updateField(field: 'username', value: string): void {
    this.formData.update(data => ({ ...data, [field]: value }));
  }

  saveProfile(): void {
    this.usernameTouched.set(true);
    if (this.usernameError()) return;

    this.saving.set(true);
    this.saveMessageKey.set('');
    this.saveErrorKey.set('');

    this.userService.updateUser({
      username: this.formData().username.trim() || null,
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.saveMessageKey.set('profile.saved');
        this.toast.success('profile.saved');
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        const key = err.error?.title ?? 'profile.saveError';
        this.saveErrorKey.set(key);
        this.toast.error(key);
      }
    });
  }

  startEditEmail(): void {
    this.emailErrorKey.set('');
    this.emailMessageKey.set('');
    this.newEmail.set('');
    this.newEmailTouched.set(false);
    this.editingEmail.set(true);
  }

  cancelEditEmail(): void {
    this.editingEmail.set(false);
  }

  requestEmailChange(): void {
    this.newEmailTouched.set(true);
    if (this.newEmailError()) return;

    const email = this.newEmail().trim();

    this.emailSending.set(true);
    this.emailErrorKey.set('');
    this.emailMessageKey.set('');

    this.userService.requestEmailChange(email).subscribe({
      next: () => {
        this.emailSending.set(false);
        this.editingEmail.set(false);
        this.emailMessageKey.set('profile.emailCodeSent');
        this.toast.success('profile.emailCodeSent');
        this.userService.refreshCurrentUser().subscribe();
      },
      error: (err: HttpErrorResponse) => {
        this.emailSending.set(false);
        const key = err.error?.title ?? 'profile.emailChangeError';
        this.emailErrorKey.set(key);
        this.toast.error(key);
      }
    });
  }

  confirmEmailChange(): void {
    this.confirmCodeTouched.set(true);
    if (this.confirmCodeError()) return;

    const code = this.confirmCode().trim();

    this.confirmingEmail.set(true);
    this.confirmErrorKey.set('');

    this.userService.confirmEmailChange(code).subscribe({
      next: () => {
        this.confirmingEmail.set(false);
        this.confirmCode.set('');
        this.emailMessageKey.set('profile.emailChanged');
        this.toast.success('profile.emailChanged');
      },
      error: (err: HttpErrorResponse) => {
        this.confirmingEmail.set(false);
        const key = err.error?.title ?? 'profile.emailConfirmError';
        this.confirmErrorKey.set(key);
        this.toast.error(key);
      }
    });
  }

  cancelEmailChange(): void {
    this.userService.cancelEmailChange().subscribe({
      next: () => {
        this.confirmCode.set('');
        this.confirmErrorKey.set('');
        this.emailMessageKey.set('');
        this.toast.success('toast.emailChangeCancelled');
      },
      error: () => this.toast.error('toast.actionFailed')
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
        this.toast.success('toast.pictureUpdated');
      },
      error: (err: HttpErrorResponse) => {
        this.uploadingPicture.set(false);
        const key = err.error?.title ?? 'profile.pictureUploadError';
        this.pictureErrorKey.set(key);
        this.toast.error(key);
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
        this.toast.success('toast.pairingCodeGenerated');
      },
      error: (err: HttpErrorResponse) => {
        this.pairingLoading.set(false);
        const key = err.error?.title ?? 'profile.pairingCodeError';
        this.pairErrorKey.set(key);
        this.toast.error(key);
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
    this.pairInputTouched.set(true);

    if (this.pairInputError()) return;

    const code = this.pairInput().trim();

    this.userService.pair({
      code,
      relationshipStartDate: this.relationshipDateInput() || null,
    }).subscribe({
      next: () => {
        this.pairingCode.set(null);
        this.pairInput.set('');
        this.toast.success('toast.pairSuccess');
      },
      error: (err: HttpErrorResponse) => {
        const key = err.error?.title ?? 'profile.pairError';
        this.pairErrorKey.set(key);
        this.toast.error(key);
      }
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
        this.toast.success('toast.relationshipDateSaved');
      },
      error: (err: HttpErrorResponse) => {
        this.relationshipDateSaving.set(false);
        const key = err.error?.title ?? 'profile.dateError';
        this.relationshipDateErrorKey.set(key);
        this.toast.error(key);
      }
    });
  }

  confirmUnpair(): void {
    this.unpairConfirming.set(true);
  }

  cancelUnpair(): void {
    this.unpairConfirming.set(false);
  }

  unpair(): void {
    this.unpairing.set(true);
    this.unpairErrorKey.set('');

    this.userService.unpair().subscribe({
      next: () => {
        this.unpairing.set(false);
        this.unpairConfirming.set(false);
        this.toast.success('toast.unpairSuccess');
      },
      error: (err: HttpErrorResponse) => {
        this.unpairing.set(false);
        const key = err.error?.title ?? 'profile.unpairError';
        this.unpairErrorKey.set(key);
        this.toast.error(key);
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
        this.toast.success('toast.accountDeleted');
        this.authService.logout().subscribe(() => this.router.navigate(['/login']));
      },
      error: (err: HttpErrorResponse) => {
        const key = err.error?.title ?? 'profile.deleteError';
        this.deleteErrorKey.set(key);
        this.toast.error(key);
      }
    });
  }
}
