using System;

namespace Vitalis.Domain.Exceptions;

public abstract class VitalisException : Exception
{
    protected VitalisException(string mensaje, string? codigo = null) : base(mensaje)
    {
        Codigo = codigo;
    }

    /// <summary>
    /// Código estable que el frontend puede usar para decidir qué mostrar
    /// (por ejemplo, ofrecer "reenviar correo" ante EMAIL_NO_CONFIRMADO) sin
    /// depender del texto del mensaje.
    /// </summary>
    public string? Codigo { get; }
}

public class NotFoundException : VitalisException
{
    public NotFoundException(string mensaje) : base(mensaje) { }
}

public class ConflictException : VitalisException
{
    public ConflictException(string mensaje) : base(mensaje) { }
}

public class ValidationException : VitalisException
{
    public ValidationException(string mensaje) : base(mensaje) { }
}

/// <summary>
/// El usuario esta autenticado pero no tiene permiso sobre ESTE recurso en
/// particular. Se distingue de un 401 (no se sabe quien sos) y de un 400
/// (el pedido esta mal formado): aca el pedido es valido y la identidad es
/// conocida, pero el recurso pertenece a otro.
/// </summary>
public class ForbiddenException : VitalisException
{
    public ForbiddenException(string mensaje, string? codigo = null) : base(mensaje, codigo) { }
}

/// <summary>
/// Demasiados intentos (cuenta bloqueada temporalmente o reenvíos muy
/// seguidos). Se traduce a 429 Too Many Requests.
/// </summary>
public class DemasiadosIntentosException : VitalisException
{
    public DemasiadosIntentosException(string mensaje, string? codigo = null) : base(mensaje, codigo) { }
}
