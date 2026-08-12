/**
 * app.js – Cliente de API y Lógica de Interfaz para Vitalis
 */

const API_BASE_URL = '/api';

// --- Gestión de Autenticación ---
function getToken() {
  return localStorage.getItem('vitalis_token');
}

function getUser() {
  return {
    nombre: localStorage.getItem('vitalis_userName') || 'Usuario',
    rol: localStorage.getItem('vitalis_userRole') || 'Usuario',
    email: localStorage.getItem('vitalis_userEmail') || ''
  };
}

function setSession(authResponse) {
  localStorage.setItem('vitalis_token', authResponse.token);
  localStorage.setItem('vitalis_userName', authResponse.nombreCompleto);
  localStorage.setItem('vitalis_userRole', authResponse.rol);
  localStorage.setItem('vitalis_userEmail', authResponse.email);
}

function clearSession() {
  localStorage.removeItem('vitalis_token');
  localStorage.removeItem('vitalis_userName');
  localStorage.removeItem('vitalis_userRole');
  localStorage.removeItem('vitalis_userEmail');
}

function checkAuthAndRedirect() {
  const token = getToken();
  const isLoginPage = window.location.pathname.endsWith('login.html');

  if (!token && !isLoginPage) {
    window.location.href = 'login.html';
  } else if (token && isLoginPage) {
    window.location.href = 'index.html';
  }
}

// --- Notificaciones Toast ---
function showToast(message, type = 'success') {
  const container = document.getElementById('toastContainer');
  if (!container) return;

  const toast = document.createElement('div');
  toast.className = `toast ${type}`;
  toast.innerHTML = `
    <span>${message}</span>
    <button class="toast-close">&times;</button>
  `;

  container.appendChild(toast);

  // Trigger animation reflow
  setTimeout(() => toast.classList.add('show'), 10);

  // Event handler for close button
  toast.querySelector('.toast-close').addEventListener('click', () => {
    toast.classList.remove('show');
    setTimeout(() => toast.remove(), 300);
  });

  // Auto-remove
  setTimeout(() => {
    if (toast.parentNode) {
      toast.classList.remove('show');
      setTimeout(() => toast.remove(), 300);
    }
  }, 4000);
}

// --- Cliente HTTP Genérico ---
async function apiRequest(method, endpoint, data = null) {
  const url = `${API_BASE_URL}${endpoint}`;
  const headers = {
    'Content-Type': 'application/json'
  };

  const token = getToken();
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  const config = {
    method: method,
    headers: headers
  };

  if (data) {
    config.body = JSON.stringify(data);
  }

  try {
    const response = await fetch(url, config);

    if (response.status === 401) {
      showToast('Sesión expirada o no autorizada. Inicie sesión nuevamente.', 'error');
      clearSession();
      setTimeout(() => { window.location.href = 'login.html'; }, 1500);
      throw new Error('No autorizado');
    }

    if (!response.ok) {
      // Intentar obtener mensaje de error detallado
      let errorMessage = `Error del servidor (${response.status})`;
      try {
        const errData = await response.json();
        errorMessage = errData.mensaje || errData.message || errorMessage;
      } catch (e) {}
      throw new Error(errorMessage);
    }

    // Retorna null si no hay contenido (por ejemplo, 204 No Content)
    if (response.status === 204) {
      return null;
    }

    return await response.json();
  } catch (error) {
    console.error(`Error en API ${method} ${endpoint}:`, error);
    throw error;
  }
}

// --- Inicialización en DOMContentLoaded ---
document.addEventListener('DOMContentLoaded', () => {
  checkAuthAndRedirect();

  // Si estamos en la página de Login
  const loginForm = document.getElementById('loginForm');
  if (loginForm) {
    loginForm.addEventListener('submit', handleLogin);
    return;
  }

  // Si estamos en el Dashboard (index.html)
  const logoutBtn = document.getElementById('logoutBtn');
  if (logoutBtn) {
    logoutBtn.addEventListener('click', () => {
      clearSession();
      window.location.href = 'login.html';
    });
  }

  // Cargar datos de usuario en la UI
  const user = getUser();
  const userNameEl = document.getElementById('userName');
  const userRoleEl = document.getElementById('userRole');
  if (userNameEl && userRoleEl) {
    userNameEl.textContent = user.nombre;
    userRoleEl.textContent = user.rol;
  }

  // Configurar las pestañas
  initTabs();

  // Cargar pestaña por defecto
  loadActiveTabContent();
});

// --- Lógica del Login ---
async function handleLogin(e) {
  e.preventDefault();
  const emailInput = document.getElementById('email');
  const passwordInput = document.getElementById('password');
  const submitBtn = e.target.querySelector('button[type="submit"]');

  if (!emailInput || !passwordInput) return;

  const originalBtnText = submitBtn.innerHTML;
  submitBtn.disabled = true;
  submitBtn.innerHTML = '<span class="spinner"></span> Ingresando...';

  try {
    const data = await apiRequest('POST', '/Auth/login', {
      email: emailInput.value.trim(),
      password: passwordInput.value
    });

    setSession(data);
    showToast(`¡Bienvenido de nuevo, ${data.nombreCompleto}!`, 'success');
    setTimeout(() => {
      window.location.href = 'index.html';
    }, 1000);
  } catch (error) {
    showToast(error.message || 'Error de conexión con el servidor.', 'error');
    submitBtn.disabled = false;
    submitBtn.innerHTML = originalBtnText;
  }
}

// --- Control de Pestañas ---
let currentTab = 'pacientes';

function initTabs() {
  const tabs = document.querySelectorAll('#navTabs .tab-btn');
  tabs.forEach(tab => {
    tab.addEventListener('click', () => {
      tabs.forEach(t => t.classList.remove('active'));
      tab.classList.add('active');
      currentTab = tab.getAttribute('data-tab');
      loadActiveTabContent();
    });
  });
}

function loadActiveTabContent() {
  const contentEl = document.getElementById('tabContent');
  if (!contentEl) return;

  if (currentTab === 'pacientes') {
    renderPacientesLayout(contentEl);
  } else if (currentTab === 'obras') {
    renderObrasLayout(contentEl);
  } else if (currentTab === 'especialidades') {
    renderEspecialidadesLayout(contentEl);
  } else if (currentTab === 'profesionales') {
    renderProfesionalesLayout(contentEl);
  } else if (currentTab === 'turnos') {
    renderTurnosLayout(contentEl);
  }
}

// --- Vista de Pacientes ---
async function renderPacientesLayout(container) {
  container.innerHTML = `
    <div class="actions-bar">
      <div class="search-box">
        <input type="text" id="pacienteSearch" class="form-control" placeholder="Buscar pacientes por nombre, apellido o DNI..." />
      </div>
      <button class="btn btn-primary" id="btnNewPaciente">
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <line x1="12" y1="5" x2="12" y2="19"></line>
          <line x1="5" y1="12" x2="19" y2="12"></line>
        </svg>
        Nuevo Paciente
      </button>
    </div>
    <div id="pacientesTableContainer">
      <div class="loading-container"><span class="spinner"></span> Cargando pacientes...</div>
    </div>
  `;

  // Listener para buscar paciente con debounce sencillo
  let debounceTimeout;
  const searchInput = document.getElementById('pacienteSearch');
  searchInput.addEventListener('input', (e) => {
    clearTimeout(debounceTimeout);
    debounceTimeout = setTimeout(() => {
      fetchAndRenderPacientes(e.target.value.trim());
    }, 400);
  });

  // Listener para crear
  document.getElementById('btnNewPaciente').addEventListener('click', () => openPacienteModal());

  // Carga inicial de pacientes
  fetchAndRenderPacientes();
}

async function fetchAndRenderPacientes(searchQuery = '') {
  const container = document.getElementById('pacientesTableContainer');
  if (!container) return;

  try {
    const endpoint = searchQuery ? `/Pacientes?buscar=${encodeURIComponent(searchQuery)}` : '/Pacientes';
    const pacientes = await apiRequest('GET', endpoint);

    if (pacientes.length === 0) {
      container.innerHTML = `<div style="text-align: center; padding: 3rem; color: var(--text-secondary);">No se encontraron pacientes.</div>`;
      return;
    }

    let rowsHtml = '';
    pacientes.forEach(p => {
      const fecha = p.fechaNacimiento ? new Date(p.fechaNacimiento).toLocaleDateString('es-AR') : '-';
      const obraSocial = p.obraSocialNombre || '<span style="color: var(--text-secondary); font-style: italic;">Ninguna</span>';
      const nroAfiliado = p.numeroAfiliado || '-';
      const statusBadge = p.activo 
        ? '<span class="status-badge active">Activo</span>' 
        : '<span class="status-badge inactive">Inactivo</span>';

      rowsHtml += `
        <tr>
          <td><strong>${p.apellido}, ${p.nombre}</strong></td>
          <td>${p.dni}</td>
          <td>${fecha}</td>
          <td>${p.email || '-'}</td>
          <td>${p.telefono || '-'}</td>
          <td>${obraSocial}</td>
          <td>${nroAfiliado}</td>
          <td>${statusBadge}</td>
          <td class="actions" style="text-align: right; white-space: nowrap;">
            <button class="btn btn-secondary btn-sm" onclick="editPaciente(${p.id})">Editar</button>
            ${p.activo ? `<button class="btn btn-danger btn-sm" onclick="deletePaciente(${p.id})">Desactivar</button>` : ''}
          </td>
        </tr>
      `;
    });

    container.innerHTML = `
      <div class="table-container">
        <table>
          <thead>
            <tr>
              <th>Nombre Completo</th>
              <th>DNI</th>
              <th>F. Nacimiento</th>
              <th>Email</th>
              <th>Teléfono</th>
              <th>Obra Social</th>
              <th>Afiliado N°</th>
              <th>Estado</th>
              <th style="text-align: right;">Acciones</th>
            </tr>
          </thead>
          <tbody>
            ${rowsHtml}
          </tbody>
        </table>
      </div>
    `;
  } catch (error) {
    container.innerHTML = `<div style="text-align: center; padding: 3rem; color: var(--danger-color);">Error al cargar pacientes: ${error.message}</div>`;
  }
}

// --- Vista de Obras Sociales ---
async function renderObrasLayout(container) {
  container.innerHTML = `
    <div class="actions-bar">
      <div style="font-size: 1.1rem; font-weight: 500; color: var(--text-secondary);">Obras Sociales Registradas</div>
      <button class="btn btn-primary" id="btnNewObra">
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <line x1="12" y1="5" x2="12" y2="19"></line>
          <line x1="5" y1="12" x2="19" y2="12"></line>
        </svg>
        Nueva Obra Social
      </button>
    </div>
    <div id="obrasTableContainer">
      <div class="loading-container"><span class="spinner"></span> Cargando obras sociales...</div>
    </div>
  `;

  document.getElementById('btnNewObra').addEventListener('click', () => openObraModal());

  fetchAndRenderObras();
}

async function fetchAndRenderObras() {
  const container = document.getElementById('obrasTableContainer');
  if (!container) return;

  try {
    const obras = await apiRequest('GET', '/ObrasSociales');

    if (obras.length === 0) {
      container.innerHTML = `<div style="text-align: center; padding: 3rem; color: var(--text-secondary);">No hay obras sociales registradas.</div>`;
      return;
    }

    let rowsHtml = '';
    obras.forEach(o => {
      const statusBadge = o.activa 
        ? '<span class="status-badge active">Activa</span>' 
        : '<span class="status-badge inactive">Inactiva</span>';

      rowsHtml += `
        <tr>
          <td><strong>${o.nombre}</strong></td>
          <td><code>${o.codigo}</code></td>
          <td>${statusBadge}</td>
          <td class="actions" style="text-align: right; white-space: nowrap;">
            <button class="btn btn-secondary btn-sm" onclick="editObra(${o.id})">Editar</button>
            <button class="btn btn-danger btn-sm" onclick="deleteObra(${o.id})">Eliminar</button>
          </td>
        </tr>
      `;
    });

    container.innerHTML = `
      <div class="table-container">
        <table>
          <thead>
            <tr>
              <th>Nombre</th>
              <th>Código</th>
              <th>Estado</th>
              <th style="text-align: right;">Acciones</th>
            </tr>
          </thead>
          <tbody>
            ${rowsHtml}
          </tbody>
        </table>
      </div>
    `;
  } catch (error) {
    container.innerHTML = `<div style="text-align: center; padding: 3rem; color: var(--danger-color);">Error al cargar obras sociales: ${error.message}</div>`;
  }
}

// --- Modales e Interacciones ---
const modal = document.getElementById('formModal');
const modalTitle = document.getElementById('modalTitle');
const entityForm = document.getElementById('entityForm');
const cancelBtn = document.getElementById('cancelBtn');
const saveBtn = document.getElementById('saveBtn');
const closeModalBtn = document.getElementById('closeModalBtn');

function openModal(title, formFieldsHtml, onSaveCallback) {
  modalTitle.textContent = title;
  entityForm.innerHTML = formFieldsHtml;
  modal.classList.add('active');

  // Desenlazar listeners anteriores del botón de Guardar
  const newSaveBtn = saveBtn.cloneNode(true);
  saveBtn.parentNode.replaceChild(newSaveBtn, saveBtn);

  newSaveBtn.addEventListener('click', async () => {
    const originalBtnText = newSaveBtn.innerHTML;
    newSaveBtn.disabled = true;
    newSaveBtn.innerHTML = '<span class="spinner"></span> Guardando...';

    try {
      await onSaveCallback();
      closeModal();
    } catch (error) {
      showToast(error.message || 'Error al guardar.', 'error');
      newSaveBtn.disabled = false;
      newSaveBtn.innerHTML = originalBtnText;
    }
  });

  // Re-enlazar variables globales para el nuevo botón
  window.currentSaveBtn = newSaveBtn;
}

function closeModal() {
  modal.classList.remove('active');
  entityForm.innerHTML = '';
}

if (cancelBtn) cancelBtn.addEventListener('click', closeModal);
if (closeModalBtn) closeModalBtn.addEventListener('click', closeModal);

// Cierra modal al hacer click afuera
window.addEventListener('click', (e) => {
  if (e.target === modal) {
    closeModal();
  }
});

// --- Modal de Paciente (Nuevo / Editar) ---
async function openPacienteModal(paciente = null) {
  let obrasSociales = [];
  try {
    obrasSociales = await apiRequest('GET', '/ObrasSociales');
  } catch (e) {
    console.error('Error al precargar Obras Sociales', e);
  }

  // Filtrar solo las activas, pero incluir la asignada si está inactiva
  const activeObras = obrasSociales.filter(o => o.activa || (paciente && o.id === paciente.ObraSocialId));

  const optionsHtml = activeObras.map(o => `
    <option value="${o.id}" ${paciente && paciente.obraSocialId === o.id ? 'selected' : ''}>${o.nombre} (${o.codigo})</option>
  `).join('');

  // Formatear fecha para el input date
  let dateVal = '';
  if (paciente && paciente.fechaNacimiento) {
    dateVal = paciente.fechaNacimiento.split('T')[0];
  }

  const formHtml = `
    <div class="form-grid">
      <div class="form-group">
        <label for="pNombre">Nombre *</label>
        <input type="text" id="pNombre" class="form-control" value="${paciente ? paciente.nombre : ''}" required />
      </div>
      <div class="form-group">
        <label for="pApellido">Apellido *</label>
        <input type="text" id="pApellido" class="form-control" value="${paciente ? paciente.apellido : ''}" required />
      </div>
      <div class="form-group">
        <label for="pDni">DNI *</label>
        <input type="text" id="pDni" class="form-control" value="${paciente ? paciente.dni : ''}" ${paciente ? 'disabled' : ''} required />
      </div>
      <div class="form-group">
        <label for="pFechaNac">Fecha de Nacimiento *</label>
        <input type="date" id="pFechaNac" class="form-control" value="${dateVal}" required />
      </div>
      <div class="form-group">
        <label for="pEmail">Email</label>
        <input type="email" id="pEmail" class="form-control" value="${paciente ? (paciente.email || '') : ''}" />
      </div>
      <div class="form-group">
        <label for="pTelefono">Teléfono</label>
        <input type="text" id="pTelefono" class="form-control" value="${paciente ? (paciente.telefono || '') : ''}" />
      </div>
      <div class="form-group" style="grid-column: span 2;">
        <label for="pDireccion">Dirección</label>
        <input type="text" id="pDireccion" class="form-control" value="${paciente ? (paciente.direccion || '') : ''}" />
      </div>
      <div class="form-group">
        <label for="pObraSocial">Obra Social</label>
        <select id="pObraSocial" class="form-control">
          <option value="">Ninguna</option>
          ${optionsHtml}
        </select>
      </div>
      <div class="form-group">
        <label for="pNumAfiliado">Número de Afiliado</label>
        <input type="text" id="pNumAfiliado" class="form-control" value="${paciente ? (paciente.numeroAfiliado || '') : ''}" />
      </div>
    </div>
  `;

  const title = paciente ? 'Editar Paciente' : 'Nuevo Paciente';

  openModal(title, formHtml, async () => {
    // Validación
    const nombre = document.getElementById('pNombre').value.trim();
    const apellido = document.getElementById('pApellido').value.trim();
    const dni = document.getElementById('pDni').value.trim();
    const fechaNac = document.getElementById('pFechaNac').value;
    const email = document.getElementById('pEmail').value.trim() || null;
    const telefono = document.getElementById('pTelefono').value.trim() || null;
    const direccion = document.getElementById('pDireccion').value.trim() || null;
    const obraSocialVal = document.getElementById('pObraSocial').value;
    const obraSocialId = obraSocialVal ? parseInt(obraSocialVal) : null;
    const numeroAfiliado = document.getElementById('pNumAfiliado').value.trim() || null;

    if (!nombre || !apellido || !dni || !fechaNac) {
      throw new Error('Complete todos los campos obligatorios (*)');
    }

    if (paciente) {
      // Editar
      const payload = { nombre, apellido, email, telefono, direccion, obraSocialId, numeroAfiliado };
      await apiRequest('PUT', `/Pacientes/${paciente.id}`, payload);
      showToast('Paciente actualizado correctamente.');
    } else {
      // Crear
      const payload = { nombre, apellido, dni, fechaNacimiento: fechaNac, email, telefono, direccion, obraSocialId, numeroAfiliado };
      await apiRequest('POST', '/Pacientes', payload);
      showToast('Paciente creado correctamente.');
    }

    fetchAndRenderPacientes(document.getElementById('pacienteSearch')?.value.trim());
  });
}

// --- Trigger funciones de Paciente expuestas en window ---
window.editPaciente = async function(id) {
  try {
    const paciente = await apiRequest('GET', `/Pacientes/${id}`);
    openPacienteModal(paciente);
  } catch (error) {
    showToast(`Error al obtener paciente: ${error.message}`, 'error');
  }
};

window.deletePaciente = async function(id) {
  if (!confirm('¿Está seguro de que desea desactivar este paciente?')) return;
  try {
    await apiRequest('DELETE', `/Pacientes/${id}`);
    showToast('Paciente desactivado correctamente.');
    fetchAndRenderPacientes(document.getElementById('pacienteSearch')?.value.trim());
  } catch (error) {
    showToast(`Error al desactivar paciente: ${error.message}`, 'error');
  }
};

// --- Modal de Obra Social (Nueva / Editar) ---
function openObraModal(obra = null) {
  const formHtml = `
    <div class="form-group">
      <label for="oNombre">Nombre *</label>
      <input type="text" id="oNombre" class="form-control" value="${obra ? obra.nombre : ''}" required />
    </div>
    <div class="form-group">
      <label for="oCodigo">Código *</label>
      <input type="text" id="oCodigo" class="form-control" value="${obra ? obra.codigo : ''}" required />
    </div>
    ${obra ? `
    <div class="form-group checkbox-group" style="margin-top: 1rem;">
      <input type="checkbox" id="oActiva" ${obra.activa ? 'checked' : ''} />
      <label for="oActiva">Obra Social Activa</label>
    </div>
    ` : ''}
  `;

  const title = obra ? 'Editar Obra Social' : 'Nueva Obra Social';

  openModal(title, formHtml, async () => {
    const nombre = document.getElementById('oNombre').value.trim();
    const codigo = document.getElementById('oCodigo').value.trim();
    const activa = obra ? document.getElementById('oActiva').checked : true;

    if (!nombre || !codigo) {
      throw new Error('Complete todos los campos obligatorios (*)');
    }

    if (obra) {
      // Editar
      const payload = { nombre, codigo, activa };
      await apiRequest('PUT', `/ObrasSociales/${obra.id}`, payload);
      showToast('Obra Social actualizada correctamente.');
    } else {
      // Crear
      const payload = { nombre, codigo };
      await apiRequest('POST', '/ObrasSociales', payload);
      showToast('Obra Social creada correctamente.');
    }

    fetchAndRenderObras();
  });
}

// --- Trigger funciones de Obra Social expuestas en window ---
window.editObra = async function(id) {
  try {
    const obra = await apiRequest('GET', `/ObrasSociales/${id}`);
    openObraModal(obra);
  } catch (error) {
    showToast(`Error al obtener Obra Social: ${error.message}`, 'error');
  }
};

window.deleteObra = async function(id) {
  if (!confirm('¿Está seguro de que desea eliminar esta Obra Social? Esto puede fallar si tiene pacientes vinculados.')) return;
  try {
    await apiRequest('DELETE', `/ObrasSociales/${id}`);
    showToast('Obra Social eliminada correctamente.');
    fetchAndRenderObras();
  } catch (error) {
    showToast(`Error al eliminar Obra Social: ${error.message}`, 'error');
  }
};

// ==========================================
// --- MÓDULO ESPECIALIDADES ---
// ==========================================

async function renderEspecialidadesLayout(container) {
  container.innerHTML = `
    <div class="actions-bar">
      <div class="search-box">
        <input type="text" id="especialidadSearch" class="form-control" placeholder="Buscar especialidades..." />
      </div>
      <button class="btn btn-primary" id="btnNewEspecialidad">
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <line x1="12" y1="5" x2="12" y2="19"></line>
          <line x1="5" y1="12" x2="19" y2="12"></line>
        </svg>
        Nueva Especialidad
      </button>
    </div>
    <div id="especialidadesTableContainer">
      <div class="loading-container"><span class="spinner"></span> Cargando especialidades...</div>
    </div>
  `;

  let debounceTimeout;
  const searchInput = document.getElementById('especialidadSearch');
  searchInput.addEventListener('input', (e) => {
    clearTimeout(debounceTimeout);
    debounceTimeout = setTimeout(() => {
      fetchAndRenderEspecialidades(e.target.value.trim());
    }, 400);
  });

  document.getElementById('btnNewEspecialidad').addEventListener('click', () => openEspecialidadModal());

  fetchAndRenderEspecialidades();
}

async function fetchAndRenderEspecialidades(searchQuery = '') {
  const container = document.getElementById('especialidadesTableContainer');
  if (!container) return;

  try {
    const endpoint = searchQuery ? `/Especialidades?buscar=${encodeURIComponent(searchQuery)}` : '/Especialidades';
    const especialidades = await apiRequest('GET', endpoint);

    if (especialidades.length === 0) {
      container.innerHTML = `<div style="text-align: center; padding: 3rem; color: var(--text-secondary);">No se encontraron especialidades.</div>`;
      return;
    }

    let rowsHtml = '';
    especialidades.forEach(e => {
      rowsHtml += `
        <tr>
          <td><strong>${e.nombre}</strong></td>
          <td>${e.descripcion || '<span style="color: var(--text-secondary); font-style: italic;">Sin descripción</span>'}</td>
          <td class="actions" style="text-align: right; white-space: nowrap;">
            <button class="btn btn-secondary btn-sm" onclick="editEspecialidad(${e.id})">Editar</button>
            <button class="btn btn-danger btn-sm" onclick="deleteEspecialidad(${e.id})">Eliminar</button>
          </td>
        </tr>
      `;
    });

    container.innerHTML = `
      <div class="table-container">
        <table>
          <thead>
            <tr>
              <th>Nombre</th>
              <th>Descripción</th>
              <th style="text-align: right;">Acciones</th>
            </tr>
          </thead>
          <tbody>
            ${rowsHtml}
          </tbody>
        </table>
      </div>
    `;
  } catch (error) {
    container.innerHTML = `<div style="text-align: center; padding: 3rem; color: var(--danger-color);">Error al cargar especialidades: ${error.message}</div>`;
  }
}

function openEspecialidadModal(especialidad = null) {
  const formHtml = `
    <div class="form-group">
      <label for="eNombre">Nombre *</label>
      <input type="text" id="eNombre" class="form-control" value="${especialidad ? especialidad.nombre : ''}" required />
    </div>
    <div class="form-group">
      <label for="eDescripcion">Descripción</label>
      <input type="text" id="eDescripcion" class="form-control" value="${especialidad ? (especialidad.descripcion || '') : ''}" />
    </div>
  `;

  const title = especialidad ? 'Editar Especialidad' : 'Nueva Especialidad';

  openModal(title, formHtml, async () => {
    const nombre = document.getElementById('eNombre').value.trim();
    const descripcion = document.getElementById('eDescripcion').value.trim() || null;

    if (!nombre) {
      throw new Error('El nombre es obligatorio (*)');
    }

    if (especialidad) {
      await apiRequest('PUT', `/Especialidades/${especialidad.id}`, { nombre, descripcion });
      showToast('Especialidad actualizada correctamente.');
    } else {
      await apiRequest('POST', '/Especialidades', { nombre, descripcion });
      showToast('Especialidad creada correctamente.');
    }

    fetchAndRenderEspecialidades(document.getElementById('especialidadSearch')?.value.trim());
  });
}

window.editEspecialidad = async function(id) {
  try {
    const especialidad = await apiRequest('GET', `/Especialidades/${id}`);
    openEspecialidadModal(especialidad);
  } catch (error) {
    showToast(`Error al obtener especialidad: ${error.message}`, 'error');
  }
};

window.deleteEspecialidad = async function(id) {
  if (!confirm('¿Está seguro de que desea eliminar esta especialidad?')) return;
  try {
    await apiRequest('DELETE', `/Especialidades/${id}`);
    showToast('Especialidad eliminada correctamente.');
    fetchAndRenderEspecialidades(document.getElementById('especialidadSearch')?.value.trim());
  } catch (error) {
    showToast(`Error al eliminar especialidad: ${error.message}`, 'error');
  }
};

// ==========================================
// --- MÓDULO PROFESIONALES ---
// ==========================================

async function renderProfesionalesLayout(container) {
  container.innerHTML = `
    <div class="actions-bar">
      <div style="font-size: 1.1rem; font-weight: 500; color: var(--text-secondary);">Profesionales Médicos</div>
      <button class="btn btn-primary" id="btnNewProfesional">
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <line x1="12" y1="5" x2="12" y2="19"></line>
          <line x1="5" y1="12" x2="19" y2="12"></line>
        </svg>
        Nuevo Profesional
      </button>
    </div>
    <div id="profesionalesTableContainer">
      <div class="loading-container"><span class="spinner"></span> Cargando profesionales...</div>
    </div>
  `;

  document.getElementById('btnNewProfesional').addEventListener('click', () => openProfesionalModal());

  fetchAndRenderProfesionales();
}

async function fetchAndRenderProfesionales() {
  const container = document.getElementById('profesionalesTableContainer');
  if (!container) return;

  try {
    const profesionales = await apiRequest('GET', '/Profesionales');

    if (profesionales.length === 0) {
      container.innerHTML = `<div style="text-align: center; padding: 3rem; color: var(--text-secondary);">No hay profesionales registrados.</div>`;
      return;
    }

    let rowsHtml = '';
    profesionales.forEach(p => {
      const statusBadge = p.activo 
        ? '<span class="status-badge active">Activo</span>' 
        : '<span class="status-badge inactive">Inactivo</span>';

      rowsHtml += `
        <tr>
          <td><strong>${p.apellido}, ${p.nombre}</strong></td>
          <td><code>${p.matricula}</code></td>
          <td>${p.email}</td>
          <td>${p.especialidadNombre}</td>
          <td>${statusBadge}</td>
          <td class="actions" style="text-align: right; white-space: nowrap;">
            <button class="btn btn-secondary btn-sm" onclick="editProfesional(${p.id})">Editar</button>
            <button class="btn btn-danger btn-sm" onclick="deleteProfesional(${p.id})">Eliminar</button>
          </td>
        </tr>
      `;
    });

    container.innerHTML = `
      <div class="table-container">
        <table>
          <thead>
            <tr>
              <th>Nombre Completo</th>
              <th>Matrícula</th>
              <th>Email</th>
              <th>Especialidad</th>
              <th>Estado</th>
              <th style="text-align: right;">Acciones</th>
            </tr>
          </thead>
          <tbody>
            ${rowsHtml}
          </tbody>
        </table>
      </div>
    `;
  } catch (error) {
    container.innerHTML = `<div style="text-align: center; padding: 3rem; color: var(--danger-color);">Error al cargar profesionales: ${error.message}</div>`;
  }
}

async function openProfesionalModal(profesional = null) {
  let especialidades = [];
  try {
    especialidades = await apiRequest('GET', '/Especialidades');
  } catch (e) {
    console.error('Error al precargar Especialidades', e);
  }

  const optionsHtml = especialidades.map(e => `
    <option value="${e.id}" ${profesional && profesional.especialidadId === e.id ? 'selected' : ''}>${e.nombre}</option>
  `).join('');

  const formHtml = `
    <div class="form-group">
      <label for="profNombre">Nombre *</label>
      <input type="text" id="profNombre" class="form-control" value="${profesional ? profesional.nombre : ''}" required />
    </div>
    <div class="form-group">
      <label for="profApellido">Apellido *</label>
      <input type="text" id="profApellido" class="form-control" value="${profesional ? profesional.apellido : ''}" required />
    </div>
    <div class="form-group">
      <label for="profMatricula">Matrícula *</label>
      <input type="text" id="profMatricula" class="form-control" value="${profesional ? profesional.matricula : ''}" required />
    </div>
    <div class="form-group">
      <label for="profEmail">Email *</label>
      <input type="email" id="profEmail" class="form-control" value="${profesional ? profesional.email : ''}" required />
    </div>
    <div class="form-group">
      <label for="profEspecialidad">Especialidad *</label>
      <select id="profEspecialidad" class="form-control" required>
        <option value="">Seleccione especialidad...</option>
        ${optionsHtml}
      </select>
    </div>
    ${profesional ? `
    <div class="form-group checkbox-group" style="margin-top: 1rem;">
      <input type="checkbox" id="profActivo" ${profesional.activo ? 'checked' : ''} />
      <label for="profActivo">Profesional Activo</label>
    </div>
    ` : ''}
  `;

  const title = profesional ? 'Editar Profesional' : 'Nuevo Profesional';

  openModal(title, formHtml, async () => {
    const nombre = document.getElementById('profNombre').value.trim();
    const apellido = document.getElementById('profApellido').value.trim();
    const matricula = document.getElementById('profMatricula').value.trim();
    const email = document.getElementById('profEmail').value.trim();
    const especialidadVal = document.getElementById('profEspecialidad').value;
    const especialidadId = especialidadVal ? parseInt(especialidadVal) : null;
    const activo = profesional ? document.getElementById('profActivo').checked : true;

    if (!nombre || !apellido || !matricula || !email || !especialidadId) {
      throw new Error('Complete todos los campos obligatorios (*)');
    }

    const payload = { nombre, apellido, matricula, email, especialidadId, activo };

    if (profesional) {
      await apiRequest('PUT', `/Profesionales/${profesional.id}`, payload);
      showToast('Profesional médico actualizado correctamente.');
    } else {
      await apiRequest('POST', '/Profesionales', payload);
      showToast('Profesional médico creado correctamente.');
    }

    fetchAndRenderProfesionales();
  });
}

window.editProfesional = async function(id) {
  try {
    const profesional = await apiRequest('GET', `/Profesionales/${id}`);
    openProfesionalModal(profesional);
  } catch (error) {
    showToast(`Error al obtener profesional: ${error.message}`, 'error');
  }
};

window.deleteProfesional = async function(id) {
  if (!confirm('¿Está seguro de que desea eliminar este profesional médico?')) return;
  try {
    await apiRequest('DELETE', `/Profesionales/${id}`);
    showToast('Profesional médico eliminado correctamente.');
    fetchAndRenderProfesionales();
  } catch (error) {
    showToast(`Error al eliminar profesional: ${error.message}`, 'error');
  }
};

// ==========================================
// --- MÓDULO TURNOS ---
// ==========================================

async function renderTurnosLayout(container) {
  container.innerHTML = `
    <div class="actions-bar">
      <div style="font-size: 1.1rem; font-weight: 500; color: var(--text-secondary);">Turnos Programados</div>
      <button class="btn btn-primary" id="btnNewTurno">
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <line x1="12" y1="5" x2="12" y2="19"></line>
          <line x1="5" y1="12" x2="19" y2="12"></line>
        </svg>
        Nuevo Turno
      </button>
    </div>
    <div id="turnosTableContainer">
      <div class="loading-container"><span class="spinner"></span> Cargando turnos...</div>
    </div>
  `;

  document.getElementById('btnNewTurno').addEventListener('click', () => openTurnoModal());

  fetchAndRenderTurnos();
}

async function fetchAndRenderTurnos() {
  const container = document.getElementById('turnosTableContainer');
  if (!container) return;

  try {
    const turnos = await apiRequest('GET', '/Turnos');

    if (turnos.length === 0) {
      container.innerHTML = `<div style="text-align: center; padding: 3rem; color: var(--text-secondary);">No hay turnos agendados.</div>`;
      return;
    }

    let rowsHtml = '';
    turnos.forEach(t => {
      const fechaHora = t.fechaHora ? new Date(t.fechaHora).toLocaleString('es-AR', { dateStyle: 'short', timeStyle: 'short' }) : '-';
      const statusBadge = t.confirmado 
        ? '<span class="status-badge active">Confirmado</span>' 
        : '<span class="status-badge inactive">Pendiente</span>';

      rowsHtml += `
        <tr>
          <td><strong>${t.pacienteNombre}</strong></td>
          <td>${t.profesionalNombre}</td>
          <td>${t.obraSocialNombre}</td>
          <td>${fechaHora}</td>
          <td>${statusBadge}</td>
          <td class="actions" style="text-align: right; white-space: nowrap;">
            <button class="btn btn-secondary btn-sm" onclick="toggleConfirmarTurno(${t.id}, ${t.confirmado})">
              ${t.confirmado ? 'Marcar Pendiente' : 'Confirmar'}
            </button>
            <button class="btn btn-secondary btn-sm" onclick="editTurno(${t.id})">Reagendar</button>
            <button class="btn btn-danger btn-sm" onclick="deleteTurno(${t.id})">Cancelar</button>
          </td>
        </tr>
      `;
    });

    container.innerHTML = `
      <div class="table-container">
        <table>
          <thead>
            <tr>
              <th>Paciente</th>
              <th>Médico / Profesional</th>
              <th>Obra Social</th>
              <th>Fecha y Hora</th>
              <th>Estado</th>
              <th style="text-align: right;">Acciones</th>
            </tr>
          </thead>
          <tbody>
            ${rowsHtml}
          </tbody>
        </table>
      </div>
    `;
  } catch (error) {
    container.innerHTML = `<div style="text-align: center; padding: 3rem; color: var(--danger-color);">Error al cargar turnos: ${error.message}</div>`;
  }
}

async function openTurnoModal(turno = null) {
  let pacientes = [];
  let profesionales = [];
  let obrasSociales = [];

  try {
    pacientes = await apiRequest('GET', '/Pacientes');
    profesionales = await apiRequest('GET', '/Profesionales');
    obrasSociales = await apiRequest('GET', '/ObrasSociales');
  } catch (e) {
    console.error('Error al precargar dependencias del turno', e);
  }

  // Filtrar activos
  const activePacientes = pacientes.filter(p => p.activo || (turno && p.id === turno.pacienteId));
  const activeProfesionales = profesionales.filter(p => p.activo || (turno && p.id === turno.profesionalId));
  const activeObras = obrasSociales.filter(o => o.activa || (turno && o.id === turno.obraSocialId));

  const pOptions = activePacientes.map(p => `<option value="${p.id}" ${turno && turno.pacienteId === p.id ? 'selected' : ''}>${p.apellido}, ${p.nombre}</option>`).join('');
  const profOptions = activeProfesionales.map(p => `<option value="${p.id}" ${turno && turno.profesionalId === p.id ? 'selected' : ''}>${p.apellido}, ${p.nombre}</option>`).join('');
  const oOptions = activeObras.map(o => `<option value="${o.id}" ${turno && turno.obraSocialId === o.id ? 'selected' : ''}>${o.nombre}</option>`).join('');

  // Formatear fecha para el input datetime-local: YYYY-MM-DDThh:mm
  let dateVal = '';
  if (turno && turno.fechaHora) {
    const dateObj = new Date(turno.fechaHora);
    const tzOffset = dateObj.getTimezoneOffset() * 60000;
    const localISOTime = (new Date(dateObj - tzOffset)).toISOString().slice(0, 16);
    dateVal = localISOTime;
  }

  const formHtml = `
    ${turno ? `
    <div style="margin-bottom: 1rem; padding: 0.75rem; background: rgba(20, 184, 166, 0.05); border-radius: 8px; font-size: 0.9rem;">
      Reagendando turno para <strong>${turno.pacienteNombre}</strong> con el Dr. <strong>${turno.profesionalNombre}</strong>.
    </div>
    ` : `
    <div class="form-group">
      <label for="tPaciente">Paciente *</label>
      <select id="tPaciente" class="form-control" required>
        <option value="">Seleccione paciente...</option>
        ${pOptions}
      </select>
    </div>
    <div class="form-group">
      <label for="tProfesional">Médico / Profesional *</label>
      <select id="tProfesional" class="form-control" required>
        <option value="">Seleccione médico...</option>
        ${profOptions}
      </select>
    </div>
    <div class="form-group">
      <label for="tObraSocial">Obra Social *</label>
      <select id="tObraSocial" class="form-control" required>
        <option value="">Seleccione obra social...</option>
        ${oOptions}
      </select>
    </div>
    `}
    <div class="form-group">
      <label for="tFechaHora">Fecha y Hora *</label>
      <input type="datetime-local" id="tFechaHora" class="form-control" value="${dateVal}" required />
    </div>
    ${turno ? `
    <div class="form-group checkbox-group" style="margin-top: 1rem;">
      <input type="checkbox" id="tConfirmado" ${turno.confirmado ? 'checked' : ''} />
      <label for="tConfirmado">Turno Confirmado</label>
    </div>
    ` : ''}
  `;

  const title = turno ? 'Reagendar Turno' : 'Nuevo Turno';

  openModal(title, formHtml, async () => {
    const fechaHora = document.getElementById('tFechaHora').value;
    if (!fechaHora) {
      throw new Error('Debe especificar la fecha y hora (*)');
    }

    if (turno) {
      // Editar
      const confirmado = document.getElementById('tConfirmado').checked;
      await apiRequest('PUT', `/Turnos/${turno.id}`, { fechaHora, confirmado });
      showToast('Turno reagendado y actualizado correctamente.');
    } else {
      // Crear
      const pacienteId = parseInt(document.getElementById('tPaciente').value);
      const profesionalId = parseInt(document.getElementById('tProfesional').value);
      const obraSocialId = parseInt(document.getElementById('tObraSocial').value);

      if (!pacienteId || !profesionalId || !obraSocialId) {
        throw new Error('Complete todos los campos obligatorios (*)');
      }

      await apiRequest('POST', '/Turnos', { pacienteId, profesionalId, obraSocialId, fechaHora });
      showToast('Turno agendado correctamente.');
    }

    fetchAndRenderTurnos();
  });
}

window.editTurno = async function(id) {
  try {
    const turno = await apiRequest('GET', `/Turnos/${id}`);
    openTurnoModal(turno);
  } catch (error) {
    showToast(`Error al obtener turno: ${error.message}`, 'error');
  }
};

window.toggleConfirmarTurno = async function(id, estadoActual) {
  try {
    const turno = await apiRequest('GET', `/Turnos/${id}`);
    await apiRequest('PUT', `/Turnos/${id}`, {
      fechaHora: turno.fechaHora,
      confirmado: !estadoActual
    });
    showToast(!estadoActual ? 'Turno confirmado correctamente.' : 'Turno puesto en estado pendiente.');
    fetchAndRenderTurnos();
  } catch (error) {
    showToast(`Error al actualizar estado del turno: ${error.message}`, 'error');
  }
};

window.deleteTurno = async function(id) {
  if (!confirm('¿Está seguro de que desea cancelar y eliminar este turno?')) return;
  try {
    await apiRequest('DELETE', `/Turnos/${id}`);
    showToast('Turno cancelado y eliminado correctamente.');
    fetchAndRenderTurnos();
  } catch (error) {
    showToast(`Error al cancelar turno: ${error.message}`, 'error');
  }
};
