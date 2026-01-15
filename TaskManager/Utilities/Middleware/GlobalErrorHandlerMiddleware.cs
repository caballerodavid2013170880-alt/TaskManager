using System.Net;
using System.Text.Json;

public class GlobalErrorHandlerMiddleware
{
    private readonly RequestDelegate _next; //Representa el siguiente paso de la ejecución
    private readonly ILogger<GlobalErrorHandlerMiddleware> _logger; //Para registrar errores  (INVESTIGAR)

    public GlobalErrorHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalErrorHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context) //Ejecución en cada petición HTTP
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);       //Permite registrar errores con mensajes controlados
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(
        HttpContext context,
        Exception exception)
    {//El contexto de la respuesta y la excepción, se gestiona la respuesta de lo que se esta consumiendo
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = new //Objeto anonimo por ello no se define con nombre
        {
            message = "Ocurrió un error inesperado.",
            detail = exception.Message
        };

        return context.Response.WriteAsync(
            JsonSerializer.Serialize(response));
    }
}