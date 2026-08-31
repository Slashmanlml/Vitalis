import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

/**
 * Ficha del paciente: la pantalla que ata todo el sistema.
 *
 * ESTE ARCHIVO ES UN ESQUELETO. La ruta y la entrada de menú ya están
 * conectadas; falta el contenido. La especificación completa está en
 * docs/19-tareas-quinta-ronda.md, sección Gemini.
 *
 * Lo único que no se negocia: cada pestaña respeta el rol de quien mira. La
 * pestaña clínica solo existe para el rol Medico. Ni recepción ni administración
 * ven diagnósticos.
 */
@Component({
  selector: 'app-paciente-ficha',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './paciente-ficha.html',
  styleUrl: './paciente-ficha.css'
})
export class PacienteFichaComponent implements OnInit {
  pacienteId = 0;

  constructor(private ruta: ActivatedRoute, private router: Router) {}

  ngOnInit(): void {
    this.pacienteId = Number(this.ruta.snapshot.paramMap.get('id')) || 0;
  }

  volver(): void {
    this.router.navigate(['/dashboard/pacientes']);
  }
}
