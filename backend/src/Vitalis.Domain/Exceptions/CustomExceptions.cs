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
