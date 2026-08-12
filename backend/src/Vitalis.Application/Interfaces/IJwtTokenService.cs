using Vitalis.Domain.Entities;

namespace Vitalis.Application.Interfaces;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAt) GenerateToken(Usuario usuario);
}
