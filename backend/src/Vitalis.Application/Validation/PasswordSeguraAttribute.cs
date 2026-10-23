using System.ComponentModel.DataAnnotations;

namespace Vitalis.Application.Validation;

/// <summary>
/// Política mínima de contraseñas: 8 caracteres o más, con al menos una
/// mayúscula, una minúscula y un número.
///
/// Antes cada DTO tenía su propia regla (MinLength(6) al cambiar la
/// contraseña, ninguna al crear un usuario). Con una sola regla aplicada en
/// todos lados no hay un camino "flojo" por donde entrar una contraseña
/// débil. Se valida en el servidor; la pantalla repite la regla solo para
/// avisar antes de enviar.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class PasswordSeguraAttribute : ValidationAttribute
{
    public const int LongitudMinima = 8;
    public const int LongitudMaxima = 128;

    public const string Descripcion =
        "La contraseña debe tener entre 8 y 128 caracteres, con al menos una mayúscula, una minúscula y un número.";

    public PasswordSeguraAttribute() : base(Descripcion) { }

    public static bool EsSegura(string? valor)
    {
        if (string.IsNullOrEmpty(valor)) return false;
        if (valor.Length < LongitudMinima || valor.Length > LongitudMaxima) return false;
        return valor.Any(char.IsUpper) && valor.Any(char.IsLower) && valor.Any(char.IsDigit);
    }

    public override bool IsValid(object? value) => value is string s && EsSegura(s);
}
