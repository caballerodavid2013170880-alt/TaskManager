using System.Diagnostics;
using System.Net;
using System.Text.Json;
using TaskManagerAPI.Utilities.Exceptions;

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
        var statusCode = exception is BusinessException be ? be.StatusCode 
            : StatusCodes.Status500InternalServerError; // 15 ene: Distinguir entre una excepción personalizada y las demás (operador ternario)

        context.Response.StatusCode = statusCode;  

        var response = new //Objeto anonimo por ello no se define con nombre
        {
            message = "Ocurrió un error inesperado.", // Lo que se manda al lado del usuario
            detail = exception.Message // Traza técnica de cuál fue el error(rastreable para diagnóstico
        };

        return context.Response.WriteAsync(
            JsonSerializer.Serialize(response));
    }
}