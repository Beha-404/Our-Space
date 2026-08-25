using System.Net;

namespace OurSpace.API.Common.Exceptions;

public abstract class ApiException(string message, HttpStatusCode statusCode) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}

public class NotFoundException(string message) : ApiException(message, HttpStatusCode.NotFound);

public class ConflictException(string message) : ApiException(message, HttpStatusCode.Conflict);

public class BadRequestException(string message) : ApiException(message, HttpStatusCode.BadRequest);

public class UnauthorizedAppException(string message) : ApiException(message, HttpStatusCode.Unauthorized);
