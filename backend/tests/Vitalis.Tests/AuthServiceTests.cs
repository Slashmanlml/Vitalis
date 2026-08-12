using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vitalis.Application.DTOs.Auth;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Entities;
using Vitalis.Infrastructure.Data;
using Vitalis.Infrastructure.Services;
using Xunit;

namespace Vitalis.Tests;

public class FakeJwtTokenService : IJwtTokenService
{
    public (string Token, DateTime ExpiresAt) GenerateToken(Usuario usuario)
    {
        return ($"fake-token-for-{usuario.Email}", DateTime.UtcNow.AddHours(8));
    }
}

public class AuthServiceTests
{
    private readonly IAuthService _service;
    private readonly VitalisDbContext _context;
    private const string PasswordPlano = "Admin123!";

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<VitalisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new VitalisDbContext(options, new Microsoft.AspNetCore.Http.HttpContextAccessor());
        _service = new AuthService(_context, new FakeJwtTokenService());

        SeedUsuario();
    }

    private void SeedUsuario()
    {
        var rol = new Rol { Id = 1, Nombre = "Administrador", Descripcion = "Rol Administrador" };
        _context.Roles.Add(rol);

        var usuario = new Usuario
        {
            Id = 1,
            Nombre = "Admin",
            Apellido = "Vitalis",
            Email = "admin@vitalis.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(PasswordPlano),
            RolId = 1,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        _context.Usuarios.Add(usuario);
        _context.SaveChanges();
    }

    [Fact]
    public async Task LoginAsync_Should_Return_Token_When_Credentials_Are_Valid()
    {
        var result = await _service.LoginAsync(new LoginRequestDto
        {
            Email = "admin@vitalis.local",
            Password = PasswordPlano
        });

        result.Should().NotBeNull();
        result!.Email.Should().Be("admin@vitalis.local");
        result.Rol.Should().Be("Administrador");
        result.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task LoginAsync_Should_Return_Null_When_Password_Is_Wrong()
    {
        var result = await _service.LoginAsync(new LoginRequestDto
        {
            Email = "admin@vitalis.local",
            Password = "PasswordIncorrecto"
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_Should_Return_Null_When_Usuario_Is_Inactivo()
    {
        var usuarioInactivo = await _context.Usuarios.FindAsync(1);
        usuarioInactivo!.Activo = false;
        await _context.SaveChangesAsync();

        var result = await _service.LoginAsync(new LoginRequestDto
        {
            Email = "admin@vitalis.local",
            Password = PasswordPlano
        });

        result.Should().BeNull();
    }
}
