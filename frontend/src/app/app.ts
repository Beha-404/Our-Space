import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AmbientBackground } from './shared/ambient-background/ambient-background';
import { BootScreen } from './shared/boot-screen/boot-screen';
import { ToastContainer } from './shared/toast/toast-container';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, ToastContainer, AmbientBackground, BootScreen],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('frontend');
}
