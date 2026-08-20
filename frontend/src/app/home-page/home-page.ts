import { Component } from '@angular/core';
import { Navbar } from "../navbar/navbar";

@Component({
  imports: [Navbar],
  selector: 'app-home-page',
  styleUrl: './home-page.css',
  templateUrl: './home-page.html',
})
export class HomePage {}
