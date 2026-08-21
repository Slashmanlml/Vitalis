using Microsoft.EntityFrameworkCore;
using Vitalis.Application.DTOs.Auth;
using Vitalis.Application.Interfaces;
using Vitalis.Infrastructure.Data;

namespace Vitalis.Infrastructure.Services;

public class AuthService(VitalisDbContext context, IJwtTokenService jwtTokenService) : IAuthService
{
    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var usuario = await context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.Activo, cancellationToken);

        if (usuario is null || !BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash))
        {
            return null;
        }

        var (token, expiresAt) = jwtTokenService.GenerateToken(usuario);

        return new LoginResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            UsuarioId = usuario.Id,
            Email = usuario.Email,
            NombreCompleto = $"{usuario.Nombre} {usuario.Apellido}",
            Rol = usuario.Rol.Nombre
        };
    }

    public async Task<UsuarioPerfilDto?> ObtenerPerfilAsync(int usuarioId)
    {
        return await context.Usuarios
            .Include(u => u.Rol)
            .Where(u => u.Id == usuarioId)
            .Select(u => new UsuarioPerfilDto
            {
                Id = u.Id,
                Nombre = u.Nombre,
                Apellido = u.Apellido,
                Email = u.Email,
                Rol = u.Rol.Nombre
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> CambiarPasswordAsync(int usuarioId, CambiarPasswordDto dto)
    {
        var usuario = await context.Usuarios.FindAsync(usuarioId);
        if (usuario == null) return false;

        if (!BCrypt.Net.BCrypt.Verify(dto.PasswordActual, usuario.PasswordHash))
            return false;

        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.PasswordNuevo);
        await context.SaveChangesAsync();
        return true;
    }
}
