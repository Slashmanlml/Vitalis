using Microsoft.EntityFrameworkCore;
using Vitalis.Application.DTOs.Usuarios;
using Vitalis.Application.Interfaces;
using Vitalis.Infrastructure.Data;
using Vitalis.Domain.Entities;
using Vitalis.Domain.Exceptions;

namespace Vitalis.Infrastructure.Services;

public class UsuarioService : IUsuarioService
{
    private readonly VitalisDbContext _context;

    public UsuarioService(VitalisDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<UsuarioDto>> ObtenerTodosAsync(string? buscar)
    {
        var query = _context.Usuarios.Include(u => u.Rol).AsQueryable();

        if (!string.IsNullOrEmpty(buscar))
        {
            query = query.Where(u => u.Nombre.Contains(buscar) 
                                  || u.Apellido.Contains(buscar) 
                                  || u.Email.Contains(buscar));
        }

        return await query
            .Select(u => new UsuarioDto
            {
                Id = u.Id,
                Nombre = u.Nombre,
                Apellido = u.Apellido,
                Email = u.Email,
                Rol = u.Rol.Nombre, // 👈 usamos el nombre del rol
                Activo = u.Activo
            })
            .ToListAsync();
    }

    public async Task<UsuarioDto?> ObtenerPorIdAsync(int id)
    {
        var usuario = await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (usuario == null) return null;

        return new UsuarioDto
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            Apellido = usuario.Apellido,
            Email = usuario.Email,
            Rol = usuario.Rol.Nombre,
            Activo = usuario.Activo
        };
    }

    public async Task<UsuarioDto> CrearAsync(CrearUsuarioDto dto)
    {
        var rol = await _context.Roles.FirstOrDefaultAsync(r => r.Nombre == dto.Rol);
        if (rol == null) throw new ValidationException("Rol no válido.");

        var email = TokenSeguro.NormalizarEmail(dto.Email);
        if (await _context.Usuarios.AnyAsync(u => u.Email == email))
            throw new ConflictException("Ya existe un usuario con ese correo.");

        var usuario = new Usuario
        {
            Nombre = dto.Nombre,
            Apellido = dto.Apellido,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Rol = rol,
            Activo = true,
            // Las cuentas del personal las crea el administrador: no pasan por
            // la confirmación de correo del registro público.
            EmailConfirmado = true,
            FechaCreacion = DateTime.UtcNow
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        return new UsuarioDto
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            Apellido = usuario.Apellido,
            Email = usuario.Email,
            Rol = usuario.Rol.Nombre,
            Activo = usuario.Activo
        };
    }

    public async Task<UsuarioDto?> EditarAsync(int id, EditarUsuarioDto dto)
    {
        var usuario = await _context.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Id == id);
        if (usuario == null) return null;

        usuario.Nombre = dto.Nombre ?? usuario.Nombre;
        usuario.Apellido = dto.Apellido ?? usuario.Apellido;
        if (dto.Email != null)
        {
            var email = TokenSeguro.NormalizarEmail(dto.Email);
            if (email != usuario.Email && await _context.Usuarios.AnyAsync(u => u.Email == email && u.Id != id))
                throw new ConflictException("Ya existe un usuario con ese correo.");
            usuario.Email = email;
        }

        if (dto.Rol != null)
        {
            var rol = await _context.Roles.FirstOrDefaultAsync(r => r.Nombre == dto.Rol);
            if (rol == null) throw new ValidationException("Rol no válido.");
            usuario.Rol = rol;
        }

        await _context.SaveChangesAsync();

        return new UsuarioDto
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            Apellido = usuario.Apellido,
            Email = usuario.Email,
            Rol = usuario.Rol.Nombre,
            Activo = usuario.Activo
        };
    }

    public async Task<bool> DesactivarAsync(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null) return false;

        usuario.Activo = false;
        await _context.SaveChangesAsync();
        return true;
    }
}
