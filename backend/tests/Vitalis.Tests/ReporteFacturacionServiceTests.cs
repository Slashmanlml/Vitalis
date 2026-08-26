using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Vitalis.Domain.Entities;
using Vitalis.Infrastructure.Data;
using Vitalis.Infrastructure.Services;
using Xunit;

namespace Vitalis.Tests;

public class ReporteFacturacionServiceTests
{
    private readonly VitalisDbContext _context;
    private readonly ReporteFacturacionService _service;

    public ReporteFacturacionServiceTests()
    {
        var options = new DbContextOptionsBuilder<VitalisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new VitalisDbContext(options, new HttpContextAccessor());
        _service = new ReporteFacturacionService(_context);

        SeedData();
    }

    private void SeedData()
    {
        var osde = new ObraSocial { Id = 1, Nombre = "OSDE", Codigo = "OSDE", Activa = true };
        var swiss = new ObraSocial { Id = 2, Nombre = "Swiss Medical", Codigo = "SMG", Activa = true };
        _context.ObrasSociales.AddRange(osde, swiss);

        var pac1 = new Paciente
        {
            Id = 1,
            Nombre = "Juan",
            Apellido = "Perez",
            Dni = "11111111",
            FechaNacimiento = new DateTime(1985, 5, 10),
            ObraSocialId = 1, // OSDE
            Activo = true
        };

        var pac2 = new Paciente
        {
            Id = 2,
            Nombre = "Maria",
            Apellido = "Gomez",
            Dni = "22222222",
            FechaNacimiento = new DateTime(1990, 8, 20),
            ObraSocialId = 2, // Swiss Medical
            Activo = true
        };

        var pac3 = new Paciente
        {
            Id = 3,
            Nombre = "Carlos",
            Apellido = "Lopez",
            Dni = "33333333",
            FechaNacimiento = new DateTime(1975, 12, 1),
            ObraSocialId = null, // Particular
            Activo = true
        };

        _context.Pacientes.AddRange(pac1, pac2, pac3);

        var esp1 = new Especialidad { Id = 1, Nombre = "Cardiología" };
        var esp2 = new Especialidad { Id = 2, Nombre = "Dermatología" };
        _context.Especialidades.AddRange(esp1, esp2);

        var prof1 = new Profesional
        {
            Id = 1,
            Nombre = "Alejandro",
            Apellido = "Gomez",
            Matricula = "MP-101",
            EspecialidadId = 1,
            Activo = true
        };

        var prof2 = new Profesional
        {
            Id = 2,
            Nombre = "Laura",
            Apellido = "Fernandez",
            Matricula = "MP-202",
            EspecialidadId = 2,
            Activo = true
        };

        _context.Profesionales.AddRange(prof1, prof2);

        // Facturas en Agosto 2026
        var fechaAgosto = new DateTime(2026, 8, 15, 10, 0, 0, DateTimeKind.Utc);

        var f1 = new Factura { Id = 1, PacienteId = 1, Fecha = fechaAgosto, Total = 10000m, Estado = "Pagada" };
        var f2 = new Factura { Id = 2, PacienteId = 2, Fecha = fechaAgosto, Total = 20000m, Estado = "Parcial" };
        var f3 = new Factura { Id = 3, PacienteId = 3, Fecha = fechaAgosto, Total = 15000m, Estado = "Pendiente" };

        _context.Facturas.AddRange(f1, f2, f3);

        // Pagos (caso de pagos parciales en Factura 2)
        var p1 = new Pago { Id = 1, FacturaId = 1, Fecha = fechaAgosto, Importe = 10000m, MedioPago = "Transferencia" };
        var p2 = new Pago { Id = 2, FacturaId = 2, Fecha = fechaAgosto, Importe = 8000m, MedioPago = "Efectivo" };
        var p3 = new Pago { Id = 3, FacturaId = 2, Fecha = fechaAgosto.AddHours(2), Importe = 4000m, MedioPago = "Tarjeta Débito" };

        _context.Pagos.AddRange(p1, p2, p3);

        // Liquidaciones
        var l1 = new Liquidacion
        {
            Id = 1,
            ProfesionalId = 1,
            PeriodoDesde = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            PeriodoHasta = new DateTime(2026, 8, 31, 0, 0, 0, DateTimeKind.Utc),
            FechaCreacion = fechaAgosto,
            Total = 18000m,
            Estado = "Aprobada"
        };

        var l2 = new Liquidacion
        {
            Id = 2,
            ProfesionalId = 2,
            PeriodoDesde = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            PeriodoHasta = new DateTime(2026, 8, 31, 0, 0, 0, DateTimeKind.Utc),
            FechaCreacion = fechaAgosto,
            Total = 12000m,
            Estado = "Pendiente"
        };

        _context.Liquidaciones.AddRange(l1, l2);

        _context.SaveChanges();
    }

    [Fact]
    public async Task ObtenerFacturacionPorPeriodo_CalculaTotalesYDesglosePorObraSocialCorrectamente()
    {
        // Arrange
        var desde = new DateTime(2026, 8, 1);
        var hasta = new DateTime(2026, 8, 31);

        // Act
        var reporte = await _service.ObtenerFacturacionPorPeriodoAsync(desde, hasta);

        // Assert
        reporte.TotalFacturado.Should().Be(45000m); // 10000 + 20000 + 15000
        reporte.CantidadFacturas.Should().Be(3);
        reporte.PromedioPorFactura.Should().Be(15000m);

        reporte.PorObraSocial.Should().HaveCount(3);
        
        var swiss = reporte.PorObraSocial.First(o => o.ObraSocialNombre == "Swiss Medical");
        swiss.TotalFacturado.Should().Be(20000m);
        swiss.PorcentajeDelTotal.Should().BeApproximately(44.44, 0.01);

        var particular = reporte.PorObraSocial.First(o => o.ObraSocialNombre.Contains("Particular"));
        particular.TotalFacturado.Should().Be(15000m);
        particular.PorcentajeDelTotal.Should().BeApproximately(33.33, 0.01);

        var osde = reporte.PorObraSocial.First(o => o.ObraSocialNombre == "OSDE");
        osde.TotalFacturado.Should().Be(10000m);
        osde.PorcentajeDelTotal.Should().BeApproximately(22.22, 0.01);
    }

    [Fact]
    public async Task ObtenerCobranzas_ConPagosParciales_NoDuplicaConteosYCalculaSaldoPendiente()
    {
        // Arrange
        var desde = new DateTime(2026, 8, 1);
        var hasta = new DateTime(2026, 8, 31);

        // Act
        var reporte = await _service.ObtenerCobranzasAsync(desde, hasta);

        // Assert
        // Total facturado = 45.000, Total pagos = 10.000 (Transf) + 8.000 (Efec) + 4.000 (Débito) = 22.000
        reporte.TotalFacturado.Should().Be(45000m);
        reporte.TotalCobrado.Should().Be(22000m);
        reporte.SaldoPendiente.Should().Be(23000m);
        reporte.CantidadPagos.Should().Be(3);
        reporte.TasaCobranzaPorcentaje.Should().BeApproximately(48.89, 0.01);

        reporte.PorMedioPago.Should().HaveCount(3);
        var transf = reporte.PorMedioPago.First(p => p.MedioPago == "Transferencia");
        transf.TotalCobrado.Should().Be(10000m);

        var efec = reporte.PorMedioPago.First(p => p.MedioPago == "Efectivo");
        efec.TotalCobrado.Should().Be(8000m);

        var deb = reporte.PorMedioPago.First(p => p.MedioPago == "Tarjeta Débito");
        deb.TotalCobrado.Should().Be(4000m);
    }

    [Fact]
    public async Task ObtenerLiquidacionesPorPeriodo_CalculaTotalesYDesglosePorProfesional()
    {
        // Arrange
        var desde = new DateTime(2026, 8, 1);
        var hasta = new DateTime(2026, 8, 31);

        // Act
        var reporte = await _service.ObtenerLiquidacionesPorPeriodoAsync(desde, hasta);

        // Assert
        reporte.TotalLiquidado.Should().Be(30000m); // 18000 + 12000
        reporte.CantidadLiquidaciones.Should().Be(2);

        reporte.PorProfesional.Should().HaveCount(2);
        var drGomez = reporte.PorProfesional.First(p => p.ProfesionalId == 1);
        drGomez.ProfesionalNombre.Should().Be("Alejandro Gomez");
        drGomez.Especialidad.Should().Be("Cardiología");
        drGomez.TotalLiquidado.Should().Be(18000m);
        drGomez.PorcentajeDelTotal.Should().Be(60.0);
    }

    [Fact]
    public async Task ObtenerResumenFinanciero_IntegraFacturacionCobranzasYLiquidaciones()
    {
        // Arrange
        var desde = new DateTime(2026, 8, 1);
        var hasta = new DateTime(2026, 8, 31);

        // Act
        var resumen = await _service.ObtenerResumenFinancieroAsync(desde, hasta);

        // Assert
        resumen.TotalFacturado.Should().Be(45000m);
        resumen.TotalCobrado.Should().Be(22000m);
        resumen.SaldoPendiente.Should().Be(23000m);
        resumen.TotalLiquidado.Should().Be(30000m);
        resumen.MargenBruto.Should().Be(15000m); // 45.000 Facturado - 30.000 Liquidado
        resumen.TopObrasSociales.Should().NotBeEmpty();
        resumen.MediosPago.Should().NotBeEmpty();
        resumen.TopLiquidacionesProfesionales.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ObtenerFacturacionPorPeriodo_PeriodoSinFacturas_DevuelveEstructuraVaciaConCeros()
    {
        // Arrange
        var desde = new DateTime(2025, 1, 1);
        var hasta = new DateTime(2025, 1, 31);

        // Act
        var reporte = await _service.ObtenerFacturacionPorPeriodoAsync(desde, hasta);

        // Assert
        reporte.TotalFacturado.Should().Be(0m);
        reporte.CantidadFacturas.Should().Be(0);
        reporte.PromedioPorFactura.Should().Be(0m);
        reporte.PorObraSocial.Should().BeEmpty();
    }
}
