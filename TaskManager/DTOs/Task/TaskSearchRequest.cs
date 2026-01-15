using System.ComponentModel.DataAnnotations;

namespace TaskManager.DTOs.Task
{
    public class TaskSearchRequest
    {
        [StringLength(50, MinimumLength = 5, ErrorMessage = "La busqueda debe tenerl al menos 3 letras")] // 6enero
        public string? text { get; set; }
        public bool? completed { get; set; }
        public int? step { get; set; }
        public string? orderBy { get; set; }


        // Se agregan "page" y "pageSize" para paginación Ejercicio 6 enero y sus Data Annotations
        [Range(1, int.MaxValue, ErrorMessage = "El número de página debe ser mayor a 0")]
        public int page { get; set; } = 1; // por defecto página 1
        [Range(1, 50, ErrorMessage = "El tamaño de página debe estar entre y X")]
        public int pageSize { get; set; } = 10; // por defecto 10 registros
    }
}