using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Vitalis.Application.DTOs.Pacientes;
using Xunit;

namespace Vitalis.Tests;

/// <summary>
/// El alta de pacientes desde recepción valida el DNI también en el servidor:
/// la pantalla ya lo hacía, pero alguien que llame a la API directo no pasa
/// por la pantalla.
/// </summary>
public class ValidacionPacienteDtoTests
{
    private static List<ValidationResult> Validar(string dni)
    {
        var dto = new CrearPacienteDto
        {
            Nombre = "Ana", Apellido = "López", Dni = dni, FechaNacimiento = new DateTime(1985, 5, 5)
        };
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), resultados, validateAllProperties: true);
        return resultados;
    }

    [Theory]
    [InlineData("12345678")]
    [InlineData("1234567")]
    [InlineData("123456")]
    public void Dni_SoloNumeros_EsValido(string dni) => Validar(dni).Should().BeEmpty();

    [Theory]
    [InlineData("12ab456")]
    [InlineData("12.345.678")]
    [InlineData("12345")]
    [InlineData("1234567890")]
    [InlineData("12 345 678")]
    public void Dni_ConLetrasSignosOLargoIncorrecto_EsInvalido(string dni) =>
        Validar(dni).Should().ContainSingle(r => r.MemberNames.Contains("Dni"));
}
