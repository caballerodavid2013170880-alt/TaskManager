namespace TaskManagerAPI.Utilities.Exceptions
{
    public class BusinessException : Exception // Clase que hereda de Excepcion
    {
        public int StatusCode { get; }

        public BusinessException(string message, int statusCode = 400)
            : base(message)
        {
            StatusCode = statusCode;
        }
    }

}