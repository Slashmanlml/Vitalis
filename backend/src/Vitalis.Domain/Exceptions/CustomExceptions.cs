using System;

namespace Vitalis.Domain.Exceptions;

public abstract class VitalisException : Exception
{
    protected VitalisException(string mensaje) : base(mensaje) { }
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
    public ForbiddenException(string mensaje) : base(mensaje) { }
}
