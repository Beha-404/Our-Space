import { Component, inject } from '@angular/core';
import { TranslatePipe } from '../../i18n/translate.pipe';
import { ToastService } from './toast.service';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './toast-container.html',
  styleUrl: './toast-container.css',
})
export class ToastContainer {
  toastService = inject(ToastService);
}
