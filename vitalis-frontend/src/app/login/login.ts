import { Component } from '@angular/core';
import { Router } from '@angular/router'; // 👈 inyecta Router para redirección
import { FormsModule } from '@angular/forms';   // 👈 habilita ngModel
import { CommonModule } from '@angular/common'; // 👈 habilita directivas básicas (*ngIf, *ngFor)
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,              // 👈 Angular 17 standalone
  imports: [CommonModule, FormsModule], // 👈 acá se declaran los módulos que usa
  templateUrl: './login.html',
  styleUrls: ['./login.css']
})
export class LoginComponent {
  email: string = '';
  password: string = '';
  errorMsg: string = '';
  isLoading: boolean = false;

  constructor(
    private authService: AuthService,
    private router: Router // 👈 inyecta Router
  ) {}

  login() {
    if (!this.email || !this.password) return;

    this.isLoading = true;
    this.errorMsg = '';

    this.authService.login(this.email, this.password).subscribe({
      next: (res) => {
        this.isLoading = false;
        localStorage.setItem('token', res.token);
        this.router.navigate(['/dashboard']); // 👈 redirección fluida a Dashboard
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMsg = 'Correo electrónico o contraseña incorrectos.';
        console.error('Error en autenticación:', err);
      }
    });
  }
}

