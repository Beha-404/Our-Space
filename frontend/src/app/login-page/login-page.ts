import { Component, signal } from '@angular/core';

@Component({
  imports: [],
  selector: 'app-login-page',
  styleUrl: './login-page.css',
  templateUrl: './login-page.html',
})
export class LoginPage {

  loginData = signal({
    username: '',
    password: '',
  });

  login(){

  }

  register(){
    
  }
}
