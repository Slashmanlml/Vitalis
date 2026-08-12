import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { EmailService, EmailLog } from '../services/email.service';
import { ToastService } from '../services/toast.service';

@Component({
  selector: 'app-email-logs',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './email-logs.html',
  styleUrls: ['./email-logs.css']
})
export class EmailLogsComponent implements OnInit {
  emails: EmailLog[] = [];
  selectedEmail: EmailLog | null = null;
  cargando = false;

  constructor(
    private emailService: EmailService,
    private toastService: ToastService
  ) {}

  ngOnInit() {
    this.cargarEmails();
  }

  cargarEmails() {
    this.cargando = true;
    this.emailService.obtenerTodos().subscribe({
      next: (data) => {
        this.emails = data || [];
        if (this.emails.length > 0) {
          this.selectedEmail = this.emails[0];
        }
        this.cargando = false;
      },
      error: (err) => {
        console.error('Error al obtener emails:', err);
        this.toastService.error('Error al cargar logs de emails');
        this.cargando = false;
      }
    });
  }

  seleccionarEmail(email: EmailLog) {
    this.selectedEmail = email;
  }
}
