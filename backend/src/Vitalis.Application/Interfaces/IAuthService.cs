using Vitalis.Application.DTOs.Auth;

namespace Vitalis.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<UsuarioPerfilDto?> ObtenerPerfilAsync(int usuarioId);
    Task<bool> CambiarPasswordAsync(int usuarioId, CambiarPasswordDto dto);
}
