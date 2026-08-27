using Microsoft.EntityFrameworkCore;
using Vitalis.Application.DTOs.Facturas;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Entities;
using Vitalis.Domain.Exceptions;
using Vitalis.Infrastructure.Data;

namespace Vitalis.Infrastructure.Services;

public class FacturaService : IFacturaService
{
    private readonly VitalisDbContext _context;

    public FacturaService(VitalisDbContext context) => _context = context;

    public async Task<List<FacturaDto>> ObtenerTodasAsync()
    {
        return await _context.Facturas
            .Include(f => f.Paciente)
            .Include(f => f.Detalles).ThenInclude(d => d.Prestacion)
            .Include(f => f.Pagos)
            .OrderByDescending(f => f.Fecha)
            .Select(f => new FacturaDto
            {
                Id = f.Id,
                PacienteId = f.PacienteId,
                PacienteNombre = f.Paciente.Nombre + " " + f.Paciente.Apellido,
                Fecha = f.Fecha,
                Total = f.Total,
                Estado = f.Estado,
                Observaciones = f.Observaciones,
                Detalles = f.Detalles.Select(d => new FacturaDetalleDto
                {
                    Id = d.Id, PrestacionId = d.PrestacionId,
                    PrestacionNombre = d.Prestacion.Nombre,
                    Cantidad = d.Cantidad, PrecioUnitario = d.PrecioUnitario, Subtotal = d.Subtotal
                }).ToList(),
                Pagos = f.Pagos.Select(p => new PagoDto
                {
                    Id = p.Id, Fecha = p.Fecha, MedioPago = p.MedioPago,
                    Importe = p.Importe, Observaciones = p.Observaciones
                }).ToList()
            })
            .ToListAsync();
    }

    public async Task<List<FacturaDto>> ObtenerPorPacienteAsync(int pacienteId)
    {
        return await _context.Facturas
            .Include(f => f.Paciente)
            .Include(f => f.Detalles).ThenInclude(d => d.Prestacion)
            .Include(f => f.Pagos)
            .Where(f => f.PacienteId == pacienteId)
            .OrderByDescending(f => f.Fecha)
            .Select(f => new FacturaDto
            {
                Id = f.Id, PacienteId = f.PacienteId,
                PacienteNombre = f.Paciente.Nombre + " " + f.Paciente.Apellido,
                Fecha = f.Fecha, Total = f.Total, Estado = f.Estado,
                Observaciones = f.Observaciones,
                Detalles = f.Detalles.Select(d => new FacturaDetalleDto
                {
                    Id = d.Id, PrestacionId = d.PrestacionId,
                    PrestacionNombre = d.Prestacion.Nombre,
                    Cantidad = d.Cantidad, PrecioUnitario = d.PrecioUnitario, Subtotal = d.Subtotal
                }).ToList(),
                Pagos = f.Pagos.Select(p => new PagoDto
                {
                    Id = p.Id, Fecha = p.Fecha, MedioPago = p.MedioPago,
                    Importe = p.Importe, Observaciones = p.Observaciones
                }).ToList()
            })
            .ToListAsync();
    }

    public async Task<FacturaDto?> ObtenerPorIdAsync(int id)
    {
        return await _context.Facturas
            .Include(f => f.Paciente)
            .Include(f => f.Detalles).ThenInclude(d => d.Prestacion)
            .Include(f => f.Pagos)
            .Where(f => f.Id == id)
            .Select(f => new FacturaDto
            {
                Id = f.Id, PacienteId = f.PacienteId,
                PacienteNombre = f.Paciente.Nombre + " " + f.Paciente.Apellido,
                Fecha = f.Fecha, Total = f.Total, Estado = f.Estado,
                Observaciones = f.Observaciones,
                Detalles = f.Detalles.Select(d => new FacturaDetalleDto
                {
                    Id = d.Id, PrestacionId = d.PrestacionId,
                    PrestacionNombre = d.Prestacion.Nombre,
                    Cantidad = d.Cantidad, PrecioUnitario = d.PrecioUnitario, Subtotal = d.Subtotal
                }).ToList(),
                Pagos = f.Pagos.Select(p => new PagoDto
                {
                    Id = p.Id, Fecha = p.Fecha, MedioPago = p.MedioPago,
                    Importe = p.Importe, Observaciones = p.Observaciones
                }).ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<FacturaDto> CrearAsync(CrearFacturaDto dto)
    {
        var detalles = new List<FacturaDetalle>();
        decimal total = 0;

        foreach (var d in dto.Detalles)
        {
            var prestacion = await _context.Prestaciones.FindAsync(d.PrestacionId)
                ?? throw new NotFoundException("Prestación no encontrada.");
            var subtotal = d.Cantidad * d.PrecioUnitario;
            total += subtotal;
            detalles.Add(new FacturaDetalle
            {
                PrestacionId = d.PrestacionId,
                Cantidad = d.Cantidad,
                PrecioUnitario = d.PrecioUnitario,
                Subtotal = subtotal
            });
        }

        var factura = new Factura
        {
            PacienteId = dto.PacienteId,
            Fecha = DateTime.UtcNow,
            Total = total,
            Estado = "Pendiente",
            Observaciones = dto.Observaciones,
            Detalles = detalles
        };

        _context.Facturas.Add(factura);
        await _context.SaveChangesAsync();

        return await ObtenerPorIdAsync(factura.Id) ?? throw new NotFoundException("No se pudo recuperar la factura recién creada.");
    }

    public async Task<FacturaDto> RegistrarPagoAsync(RegistrarPagoDto dto)
    {
        var factura = await _context.Facturas
            .Include(f => f.Pagos)
            .FirstOrDefaultAsync(f => f.Id == dto.FacturaId)
            ?? throw new NotFoundException("Factura no encontrada.");

        // El importe no tenía ninguna validación. Un importe negativo se aceptaba y
        // se sumaba tal cual, de modo que bastaba registrar un pago en negativo
        // para hacer que una factura ya cobrada volviera al estado "Pago Parcial".
        // En un módulo de facturación eso no es un detalle: es corromper el
        // registro de lo cobrado.
        if (dto.Importe <= 0)
        {
            throw new ValidationException("El importe del pago debe ser mayor a cero.");
        }

        // Una factura saldada no admite pagos nuevos. Antes se aceptaban y quedaba
        // un pago colgado que no correspondía a ninguna deuda.
        if (factura.Estado == "Pagada")
        {
            throw new ConflictException("La factura ya se encuentra saldada; no admite nuevos pagos.");
        }

        // Se calcula ANTES de agregar el nuevo pago: EF Core hace fixup automático
        // de la relación (por FacturaId) y ya suma el pago nuevo a factura.Pagos en
        // cuanto se llama Add(), así que sumar después duplicaría el importe.
        var totalPagadoPrevio = factura.Pagos.Sum(p => p.Importe);

        var pago = new Pago
        {
            FacturaId = dto.FacturaId,
            Fecha = DateTime.UtcNow,
            MedioPago = dto.MedioPago,
            Importe = dto.Importe,
            Observaciones = dto.Observaciones
        };

        _context.Pagos.Add(pago);

        var totalPagado = totalPagadoPrevio + dto.Importe;
        if (totalPagado >= factura.Total)
            factura.Estado = "Pagada";
        else
            factura.Estado = "Pago Parcial";

        await _context.SaveChangesAsync();

        return await ObtenerPorIdAsync(factura.Id) ?? throw new NotFoundException("No se pudo recuperar la factura luego de registrar el pago.");
    }
}
