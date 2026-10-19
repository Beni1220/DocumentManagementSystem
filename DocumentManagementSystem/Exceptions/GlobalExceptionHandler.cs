using DocumentManagementSystem.DataAccess.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace DocumentManagementSystem.Exceptions
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var (status, message) = exception switch
            {
                DocumentNotFoundException => (StatusCodes.Status404NotFound, exception.Message),
                DataAccessException => (StatusCodes.Status500InternalServerError, "A database error occurred."),
                MessagingException => (StatusCodes.Status503ServiceUnavailable, "The message queue is currently not available. Please try again later."),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
            };

            if (status >= 500)
                _logger.LogError(exception, "Request failed with status {StatusCode}", status);
            else
                _logger.LogWarning("Request failed with status {StatusCode}: {Message}", status, exception.Message);

            httpContext.Response.StatusCode = status;
            await httpContext.Response.WriteAsJsonAsync(new { message }, cancellationToken);
            return true;
        }
    }
}
