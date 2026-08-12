using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Vitalis.Application.DTOs.Facturas;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Entities;
using Vitalis.Infrastructure.Data;
using Vitalis.Infrastructure.Services;
using Xunit;

namespace Vitalis.Tests;

public class FacturaServiceTests
{
    private readonly IFacturaService _service;
    private readonly VitalisDbContext _context;

    public FacturaServiceTests()
    {
        var options = new DbContextOptionsBuilder<VitalisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new VitalisDbContext(options, new HttpContextAccessor());
        _service = new FacturaService(_context);

        SeedRelatedEntities();
    }

    private void SeedRelatedEntities()
    {
        _context.Pacientes.Add(new Paciente
        {
            Id = 1,
            Nombre = "Juan",
            Apellido = "Perez",
            Dni = "12345678",
            FechaNacimiento = new DateTime(1990, 1, 1),
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        });

        _context.Prestaciones.Add(new Prestacion
        {
            Id = 1,
            Nombre = "Consulta Médica General",
            Codigo = "CONS-GEN",
            ImporteBase = 3000m,
            Activa = true
        });

        _context.SaveChanges();
    }

    [Fact]
    public async Task CrearAsync_Should_Calculate_Total_From_Detalles()
    {
        var dto = new CrearFacturaDto
        {
            PacienteId = 1,
            Observaciones = "Factura de prueba",
            Detalles = new List<CrearFacturaDetalleDto>
            {
                new() { PrestacionId = 1, Cantidad = 2, PrecioUnitario = 3000m }
            }
        };

        var result = await _service.CrearAsync(dto);

        result.Should().NotBeNull();
        result.Total.Should().Be(6000m);
        result.Estado.Should().Be("Pendiente");
    }

    [Fact]
    public async Task RegistrarPagoAsync_Should_Marcar_Pagada_When_Importe_Cubre_El_Total()
    {
        var factura = await _service.CrearAsync(new CrearFacturaDto
        {
            PacienteId = 1,
            Detalles = new List<CrearFacturaDetalleDto>
            {
                new() { PrestacionId = 1, Cantidad = 1, PrecioUnitario = 3000m }
            }
        });

        var result = await _service.RegistrarPagoAsync(new RegistrarPagoDto
        {
            FacturaId = factura.Id,
            MedioPago = "Efectivo",
            Importe = 3000m
        });

        result.Estado.Should().Be("Pagada");
    }

    [Fact]
    public async Task RegistrarPagoAsync_Should_Marcar_PagoParcial_When_Importe_No_Cubre_El_Total()
    {
        var factura = await _service.CrearAsync(new CrearFacturaDto
        {
            PacienteId = 1,
            Detalles = new List<CrearFacturaDetalleDto>
            {
                new() { PrestacionId = 1, Cantidad = 2, PrecioUnitario = 3000m }
            }
        });

        var result = await _service.RegistrarPagoAsync(new RegistrarPagoDto
        {
            FacturaId = factura.Id,
            MedioPago = "Efectivo",
            Importe = 3000m
        });

        result.Estado.Should().Be("Pago Parcial");
    }
}
