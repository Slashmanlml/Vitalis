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
using Vitalis.Domain.Exceptions;
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

    // -------------------------------------------------------------------------
    // Integridad del registro de pagos.
    //
    // Hallazgos de la auditoria de la ronda 4 (docs/16). El importe no tenia
    // ninguna validacion: un pago en negativo se sumaba tal cual y podia hacer
    // que una factura ya saldada volviera a "Pago Parcial". Y una factura
    // "Pagada" aceptaba pagos nuevos que no correspondian a ninguna deuda.
    // -------------------------------------------------------------------------

    private async Task<FacturaDto> FacturaDe(decimal total)
    {
        return await _service.CrearAsync(new CrearFacturaDto
        {
            PacienteId = 1,
            Detalles = new List<CrearFacturaDetalleDto>
            {
                new() { PrestacionId = 1, Cantidad = 1, PrecioUnitario = total }
            }
        });
    }

    [Fact]
    public async Task RegistrarPagoAsync_ImporteNegativo_Rechaza()
    {
        var factura = await FacturaDe(3000m);

        var act = async () => await _service.RegistrarPagoAsync(new RegistrarPagoDto
        {
            FacturaId = factura.Id,
            MedioPago = "Efectivo",
            Importe = -500m
        });

        await act.Should().ThrowAsync<ValidationException>();
        (await _context.Pagos.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task RegistrarPagoAsync_ImporteCero_Rechaza()
    {
        var factura = await FacturaDe(3000m);

        var act = async () => await _service.RegistrarPagoAsync(new RegistrarPagoDto
        {
            FacturaId = factura.Id,
            MedioPago = "Efectivo",
            Importe = 0m
        });

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task RegistrarPagoAsync_SobreFacturaSaldada_Rechaza()
    {
        var factura = await FacturaDe(3000m);

        await _service.RegistrarPagoAsync(new RegistrarPagoDto
        {
            FacturaId = factura.Id, MedioPago = "Efectivo", Importe = 3000m
        });

        var act = async () => await _service.RegistrarPagoAsync(new RegistrarPagoDto
        {
            FacturaId = factura.Id, MedioPago = "Efectivo", Importe = 500m
        });

        await act.Should().ThrowAsync<ConflictException>();
        (await _context.Pagos.CountAsync()).Should().Be(1, "el segundo pago no debe registrarse");
    }

    [Fact]
    public async Task RegistrarPagoAsync_FacturaInexistente_LanzaNotFound()
    {
        // Antes lanzaba una Exception generica, que el middleware no sabe traducir:
        // pedir una factura inexistente devolvia 500 "error interno del servidor"
        // en vez de 404 con el motivo real.
        var act = async () => await _service.RegistrarPagoAsync(new RegistrarPagoDto
        {
            FacturaId = 9999, MedioPago = "Efectivo", Importe = 100m
        });

        await act.Should().ThrowAsync<NotFoundException>();
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
